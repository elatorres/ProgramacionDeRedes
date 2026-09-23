using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ClientProgram
{
    static async Task Main(string[] args)
    {
        string host = "127.0.0.1";
        int port = 5000;

        Console.WriteLine("=== TCP CLIENT STARTED ===");
        Console.WriteLine("Commands to try:");
        Console.WriteLine("  - Any plain text (e.g., 'Hello world')");
        Console.WriteLine("  - 'report'  (Triggers 15-second report processing on server)");
        Console.WriteLine("  - 'cancel'  (Cancels active report on server)");
        Console.WriteLine("  - 'exit'    (Disconnects client)\n");

        try
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(host, port);
            Console.WriteLine("[Client] Connected to server successfully.\n");

            using NetworkStream stream = client.GetStream();
            using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            while (true)
            {
                Console.Write("Enter message: ");
                string? message = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(message)) continue;

                if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[Client] Disconnecting...");
                    break;
                }

                await writer.WriteLineAsync(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Client] Error: {ex.Message}");
        }

        Console.WriteLine("[Client] Terminated.");
    }
}