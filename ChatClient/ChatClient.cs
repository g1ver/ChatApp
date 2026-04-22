using System.Net.Sockets;
using System.Text;

namespace ChatClient;

internal static class ChatClient
{
    private const int Port = 6767;
    private const string Server = "127.0.0.1";

    private static void Main()
    {
        using var client = new TcpClient(Server, Port);
        var stream = client.GetStream();

        Console.WriteLine($"Connected to {Server}:{Port}");

        HandleMessages(stream);

        Console.Write("> ");
        while (client.Connected)
        {
            var input = Console.ReadLine();
            if (input is null) break;
            if (input.Length == 0)
            {
                Console.Write("> ");
                continue;
            }

            byte[] data;
            var quit = false;
            if (input is "!exit")
            {
                data = "COMMAND:QUIT\n"u8.ToArray();
                quit = true;
            }
            else
            {
                data = Encoding.UTF8.GetBytes(input + '\n');
            }

            stream.Write(data, 0, data.Length);
            if (quit) break;
            Console.Write("> ");
        }
    }

    private static void HandleMessages(Stream stream)
    {
        Task.Run(() =>
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            while (reader.ReadLine() is { } line)
            {
                Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
                Console.WriteLine(line);
                Console.Write("> ");
            }
        });
    }
}