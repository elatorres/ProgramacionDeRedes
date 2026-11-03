using System;
using System.Threading.Tasks;

using Common;
// Client
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("WebSocket Communication System");
        Console.WriteLine("=============================");

        bool startServer = true;
        bool startClient = true;

        if (args.Length > 0)
        {
            foreach (var arg in args)
            {
                if (arg == "--server-only") startClient = false;
                if (arg == "--client-only") startServer = false;
            }
        }
        
        startServer = false;
        
        WebSocketServer server = null;
        WebSocketClient client = null;

        try
        {
            if (startServer)
            {
                server = new WebSocketServer(8080);
                await server.StartAsync();
                Console.WriteLine("Server started on port 8080");
            }

            if (startClient)
            {
                await Task.Delay(1000); // Give server time to start
                client = new WebSocketClient("ws://localhost", 8080);
                await client.ConnectAsync();
                Console.WriteLine("Client connected to server");
            }

            Console.WriteLine("\nSystem is running. Press any key to stop...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            if (client != null)
            {
                await client.DisconnectAsync();
                client.Dispose();
                Console.WriteLine("Client stopped");
            }

            if (server != null)
            {
                await server.StopAsync();
                server.Dispose();
                Console.WriteLine("Server stopped");
            }
        }

        Console.WriteLine("System stopped");
    }
}