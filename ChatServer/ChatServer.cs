using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer;

internal static class ChatServer
{
    private const int Port = 6767;
    private const string ServerIp = "127.0.0.1";

    private static readonly ConcurrentDictionary<TcpClient, string> Clients = [];

    private static void Main()
    {
        TcpListener? server = null;
        try
        {
            var localAddr = IPAddress.Parse(ServerIp);
            server = new TcpListener(localAddr, Port);
            server.Start();

            Console.WriteLine("Waiting for a connections...");
            while (true)
            {
                var client = server.AcceptTcpClient();
                Task.Run(() => HandleClient(client));
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
        finally
        {
            server?.Stop();
        }
    }

    private static void HandleClient(TcpClient client)
    {
        SendWelcomeMessage(client);

        try
        {
            var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var username = PromptUsername(client, reader);
            if (username == string.Empty) return;

            SendMessage(client, $"Server: Your username is: {username}.");
            BroadcastClientJoinMessage(client, username);
            AddClient(client, username);

            Console.WriteLine($"Client connected: {username} ({client.Client.RemoteEndPoint})");

            while (reader.ReadLine() is { } data)
            {
                if (data.Equals("COMMAND:QUIT")) break;
                BroadcastClientMessage(client, data);
                Console.WriteLine($"{GetUsername(client)} ({client.Client.RemoteEndPoint}): {data}");
            }

            BroadcastClientLeaveMessage(client);
            Console.WriteLine($"Client disconnected: {username} ({client.Client.RemoteEndPoint})");
            RemoveClient(client);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: {0}", e);
        }
    }

    private static string PromptUsername(TcpClient client, StreamReader reader)
    {
        SendMessage(client, "Server: Enter your username below.");

        while (reader.ReadLine() is { } data)
        {
            if (!string.IsNullOrWhiteSpace(data) &&
                !Clients.Values.Any(u => u.Equals(data, StringComparison.OrdinalIgnoreCase))) return data;
            SendMessage(client, "Server: That username is invalid or already in use! Try again.");
        }

        return string.Empty; // client disconnected before choosing a name
    }

    private static void BroadcastMessage(string message)
    {
        foreach (var (client, _) in GetClientsSnapshot())
        {
            SendMessage(client, message);
        }
    }

    private static void BroadcastClientMessage(TcpClient client, string message)
    {
        BroadcastMessage($"{GetUsername(client)}: {message}");
    }

    private static IEnumerable<KeyValuePair<TcpClient, string>> GetClientsSnapshot()
    {
        return Clients.ToArray();
    }

    private static void SendMessage(TcpClient client, string message)
    {
        var data = Encoding.UTF8.GetBytes($"{message}\n");
        client.GetStream().Write(data, 0, data.Length);
    }

    private static void BroadcastClientJoinMessage(TcpClient client, string username)
    {
        BroadcastMessage($"Server: {username} ({client.Client.RemoteEndPoint}) has joined the chat.");
    }

    private static void BroadcastClientLeaveMessage(TcpClient client)
    {
        BroadcastMessage($"Server: {GetUsername(client)} ({client.Client.RemoteEndPoint}) has left the chat.");
    }

    private static void SendWelcomeMessage(TcpClient client)
    {
        SendMessage(client, $"Server: Welcome to the chat! You are connecting from {client.Client.RemoteEndPoint}.");
    }

    private static void AddClient(TcpClient client, string username)
    {
        Clients.TryAdd(client, username);
    }

    private static void RemoveClient(TcpClient client)
    {
        Clients.TryRemove(client, out _);
        client.Close();
    }

    private static string GetUsername(TcpClient client)
    {
        return Clients.GetValueOrDefault(client, client.Client.RemoteEndPoint?.ToString() ?? "Unknown");
    }
}