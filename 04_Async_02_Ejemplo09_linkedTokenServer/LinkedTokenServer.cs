using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class ServerProgram
{
    private static readonly ConcurrentBag<Task> ClientTasks = new ConcurrentBag<Task>();

    static async Task Main(string[] args)
    {
        int port = 5000;
        using CancellationTokenSource cts = new CancellationTokenSource();

        Console.WriteLine("=== TCP SERVER STARTED ===");
        Console.WriteLine("Type 'exit' in the server console to shut down.\n");

        // Start reading operator input from console
        Task consoleTask = ReadServerConsoleAsync(cts);

        // Start listening for TCP clients
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                // Accept incoming TCP clients asynchronously
                TcpClient client = await listener.AcceptTcpClientAsync(cts.Token);
                
                // Fire and forget ClientHandlerAsync, tracking the running task
                Task clientTask = HandleClientAsync(client, cts.Token);
                ClientTasks.Add(clientTask);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Server] Stop listening signal received.");
        }
        finally
        {
            listener.Stop();
        }

        Console.WriteLine("[Server] Waiting for active client tasks to finish...");
        
        // Ensure all active client tasks and the console reader task complete
        try
        {
            await Task.WhenAll(ClientTasks);
            await consoleTask;
        }
        catch (Exception ex) when (ex is OperationCanceledException or AggregateException)
        {
            // Expected during cancellation shutdown
        }

        Console.WriteLine("[Server] All tasks finished. Server terminated gracefully.");
    }

    private static async Task ReadServerConsoleAsync(CancellationTokenSource cts)
    {
        await Task.Yield(); // Shift to background thread pool
        while (!cts.Token.IsCancellationRequested)
        {
            string? input = Console.ReadLine();
            if (input?.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.WriteLine("[Server] 'exit' command entered by operator. Cancelling all server tasks...");
                cts.Cancel();
                break;
            }
        }
    }

    private static async Task HandleClientAsync(TcpClient client, CancellationToken serverCancellationToken)
    {
        string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown Client";
        Console.WriteLine($"[Server] Client connected: {clientEndpoint}");

        using (client)
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            // CTS for controlling report generation specifically for this client
            using CancellationTokenSource clientReportCts = new CancellationTokenSource();
            Task? activeReportTask = null;

            try
            {
                while (!serverCancellationToken.IsCancellationRequested)
                {
                    string? message = await reader.ReadLineAsync(serverCancellationToken);
                    if (message == null) break; // Client disconnected

                    Console.WriteLine($"[{clientEndpoint}]: {message}");

                    if (message.Equals("report", StringComparison.OrdinalIgnoreCase))
                    {
                        // Create a linked token so report task cancels if EITHER client sends 'cancel' OR server exits
                        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                            serverCancellationToken, 
                            clientReportCts.Token);

                        Console.WriteLine($"[Server] Starting report generation for {clientEndpoint}...");
                        activeReportTask = GenerateReportAsync(clientEndpoint, linkedCts.Token);
                    }
                    else if (message.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[Server] Received 'cancel' command from {clientEndpoint}.");
                        clientReportCts.Cancel(); // Triggers cancellation for this client's active report task
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[Server] Handler for {clientEndpoint} interrupted by server shutdown.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Error handling {clientEndpoint}: {ex.Message}");
            }
            finally
            {
                // Ensure report task completes or cancels cleanly before exiting client handler
                if (activeReportTask != null)
                {
                    try { await activeReportTask; } catch { }
                }
                Console.WriteLine($"[Server] Client disconnected: {clientEndpoint}");
            }
        }
    }

    private static async Task GenerateReportAsync(string clientLabel, CancellationToken cancellationToken)
    {
        try
        {
            // Simulate heavy report generation in 1-second ticks over X seconds
            for (int i = 1; i <= 15; i++)
            {
                // Delay asynchronously while periodically checking cancellation status
                await Task.Delay(1000, cancellationToken);
                Console.WriteLine($"[Report Generation - {clientLabel}] Processing step {i}/5...");
            }

            Console.WriteLine($"\n==========================================");
            Console.WriteLine($"  REPORT PRODUCED FOR {clientLabel}");
            Console.WriteLine($"  Status: SUCCESS | Time: {DateTime.Now:T}");
            Console.WriteLine($"==========================================\n");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[Report Generation - {clientLabel}] CANCELLED by request or server exit.");
        }
    }
}