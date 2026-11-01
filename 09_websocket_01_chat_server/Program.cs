using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace ChatServer;

internal static class Program
{
    // Active clients: socket -> username
    private static readonly ConcurrentDictionary<WebSocket, string> Clients = new();
    private static readonly CancellationTokenSource ShutdownCts = new();

    private static async Task Main()
    {
        const string url = "http://localhost:5000/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(url);

        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"[SERVER] Could not start listener on {url}: {ex.Message}");
            Console.WriteLine("[SERVER] Check if port is available or other service is using it.");
            return;
        }

        Console.WriteLine($"[SERVER] Listening on {url}");
        Console.WriteLine("[SERVER] Type 'exit' + Enter to stop the server.");

        // Console watcher task: stop server on "exit"
        _ = Task.Run(async () =>
        {
            while (true)
            {
                var line = Console.ReadLine();
                if (line != null && line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[SERVER] Shutdown requested from console...");
                    ShutdownCts.Cancel();
                    return;
                }
            }
        });
        
        // Loop to accept connections
        var acceptLoop = AcceptLoopAsync(listener, ShutdownCts.Token);
        
        // Wait till the end of time...
        try
        {
            await acceptLoop;
        }
        catch (OperationCanceledException) { /* expected */ }

        // Broadcast shutdown notice
        await BroadcastAsync("*** SERVER: The chat server is going down. Goodbye! ***", null);
        await Task.Delay(1000);

        // Close all sockets
        foreach (var ws in Clients.Keys.ToList())
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.CloseAsync(WebSocketCloseStatus.EndpointUnavailable,
                        "Server shutting down", CancellationToken.None);
            }
            catch { /* ignore */ }
            Clients.TryRemove(ws, out _);
        }

        try { listener.Stop(); } catch { }
        Console.WriteLine("[SERVER] Stopped.");
    }

    private static async Task AcceptLoopAsync(HttpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await listener.GetContextAsync().WaitAsync(ct);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] Accept error: {ex.Message}");
                continue;
            }

            if (!ctx.Request.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Close();
                continue;
            }

            _ = HandleClientAsync(ctx);
        }
    }

    private static async Task HandleClientAsync(HttpListenerContext ctx)
    {
        WebSocket? ws = null;
        string userName = "(unknown)";
        try
        {
            var wsc = await ctx.AcceptWebSocketAsync(null);
            ws = wsc.WebSocket;

            // First message = username
            var first = await ReceiveTextAsync(ws, ShutdownCts.Token);
            if (first == null)
            {
                await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "No username provided", CancellationToken.None);
                return;
            }

            userName = first.Trim();
            if (string.IsNullOrWhiteSpace(userName)) userName = "guest";
            // ensure uniqueness
            var taken = Clients.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var baseName = userName;
            int n = 1;
            while (taken.Contains(userName)) userName = $"{baseName}{++n}";

            Clients[ws] = userName;
            Console.WriteLine($"[SERVER] '{userName}' connected ({Clients.Count} online).");
            await BroadcastAsync($"*** {userName} has joined the chat ***", ws);

            while (ws.State == WebSocketState.Open && !ShutdownCts.IsCancellationRequested)
            {
                var msg = await ReceiveTextAsync(ws, ShutdownCts.Token);
                if (msg == null) break;

                if (msg.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[SERVER] '{userName}' sent exit.");
                    break;
                }

                var formatted = $"{userName}: {msg}";
                Console.WriteLine($"[BROADCAST] {formatted}");
                await BroadcastAsync(formatted, ws);
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"[SERVER] WebSocket error for '{userName}': {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVER] Error for '{userName}': {ex.Message}");
        }
        finally
        {
            if (ws != null)
            {
                Clients.TryRemove(ws, out _);
                Console.WriteLine($"[SERVER] '{userName}' disconnected ({Clients.Count} online).");
                await BroadcastAsync($"*** {userName} has left the chat ***", null);
                try
                {
                    if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                }
                catch { }
                ws.Dispose();
            }
        }
    }

    private static async Task<string?> ReceiveTextAsync(WebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close) return null;
            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static async Task BroadcastAsync(string message, WebSocket? exclude)
    {
        var payload = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(payload);

        foreach (var kvp in Clients.ToArray())
        {
            var ws = kvp.Key;
            if (ws == exclude) continue;
            if (ws.State != WebSocketState.Open) continue;
            try
            {
                await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] Failed to send to '{kvp.Value}': {ex.Message}");
            }
        }
    }
}