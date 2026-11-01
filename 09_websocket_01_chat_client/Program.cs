using System.Net.WebSockets;
using System.Text;

namespace ChatClient;

internal static class Program
{
    private static async Task Main()
    {
        const string serverUri = "ws://localhost:5000/";

        Console.Write("Enter your name: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("[CLIENT] Invalid name. Exiting.");
            return;
        }

        using var ws = new ClientWebSocket();
        using var cts = new CancellationTokenSource();

        try
        {
            Console.WriteLine($"[CLIENT] Connecting to {serverUri} ...");
            await ws.ConnectAsync(new Uri(serverUri), cts.Token);
            Console.WriteLine("[CLIENT] Connected. Type messages and press Enter. Type 'exit' to leave.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLIENT] Could not connect: {ex.Message}");
            return;
        }

        // Register username
        try
        {
            await SendAsync(ws, name, cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLIENT] Failed to register name: {ex.Message}");
            return;
        }

        // Background task: receive messages
        var receiveTask = Task.Run(async () =>
        {
            var buffer = new byte[4096];
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine("[CLIENT] Server closed the connection.");
                            return;
                        }
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    var text = Encoding.UTF8.GetString(ms.ToArray());
                    Console.WriteLine(text);
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"[CLIENT] Connection lost: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Receive error: {ex.Message}");
            }
        });

        // Main loop: read user input and send
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var line = Console.ReadLine();
                if (line == null) break;

                if (line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[CLIENT] Leaving chat...");
                    await SendAsync(ws, "exit", cts.Token);
                    break;
                }

                if (string.IsNullOrEmpty(line)) continue;
                await SendAsync(ws, line, cts.Token);
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"[CLIENT] Send failed: {ex.Message}");
        }

        // Graceful close
        try
        {
            if (ws.State == WebSocketState.Open)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
        }
        catch { }

        cts.Cancel();
        try { await receiveTask; } catch { }
        Console.WriteLine("[CLIENT] Disconnected.");
    }

    private static async Task SendAsync(ClientWebSocket ws, string text, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }
}