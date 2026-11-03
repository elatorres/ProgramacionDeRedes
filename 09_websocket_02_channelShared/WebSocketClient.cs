using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Common;

public class WebSocketClient : IDisposable
{
    private WebSocketChannel _channel;
    private readonly string _serverUri;
    private readonly int _port;
    private readonly CancellationTokenSource _cts;

    public WebSocketClient(string serverUri, int port)
    {
        _serverUri = serverUri;
        _port = port;
        _cts = new CancellationTokenSource();
    }

    public async Task ConnectAsync()
    {
        try
        {
            _channel = new WebSocketChannel(_serverUri, _port);
            await _channel.ConnectAsync();
            Console.WriteLine("Connected to WebSocket server");

            // Register agents
            await CreateClientAgentsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
            throw;
        }
    }

    private async Task CreateClientAgentsAsync()
    {
        var agent1 = _channel.RegisterAgent("Client Sensor Agent");
        var agent2 = _channel.RegisterAgent("Client Monitor Agent");
        var agent3 = _channel.RegisterAgent("Client Logger Agent");

        // Set up message handlers
        agent1.MessageReceived += (sender, args) =>
        {
            Console.WriteLine($"Client Agent {agent1.Id} received from {args.SourceId}: {args.Message}");
            // return Task.CompletedTask;
        };

        agent2.MessageReceived += async (sender, args) =>
        {
            Console.WriteLine($"Client Monitor received from {args.SourceId}: {args.Message}");
            // Send acknowledgment
            await agent2.SendMessageAsync(args.SourceId, "ACK: Message received");
        };

        agent3.MessageReceived += (sender, args) =>
        {
            Console.WriteLine($"Client Logger received from {args.SourceId}: {args.Message}");
            // return Task.CompletedTask;
        };

        // Start simulating client activity
        _ = Task.Run(() => SimulateClientActivityAsync(agent1, agent2, agent3));
    }

    private async Task SimulateClientActivityAsync(params WebSocketChannelAgent[] agents)
    {
        var random = new Random();

        // First, request directory
        try
        {
            var primaryAgent = agents[0];
            await primaryAgent.SendMessageAsync(0, "directory");
            Console.WriteLine("Requested directory from server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Directory request failed: {ex.Message}");
        }

        // Simulate communication
        int messageCounter = 0;
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var source = agents[random.Next(agents.Length)];
                var destId = random.Next(1, 5); // Target server agents (1-4)

                if (source.IsConnected)
                {
                    var message = new
                    {
                        Type = "DATA",
                        MessageId = ++messageCounter,
                        Payload = $"Client message {messageCounter}",
                        Timestamp = DateTime.UtcNow
                    };

                    await source.SendMessageAsync(destId, JsonSerializer.Serialize(message));
                    Console.WriteLine($"Client sent message {messageCounter} to agent {destId}");

                    await Task.Delay(random.Next(5000, 15000));
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client simulation error: {ex.Message}");
                await Task.Delay(5000);
            }
        }
    }

    public async Task DisconnectAsync()
    {
        _cts.Cancel();
        _channel?.Dispose();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _channel?.Dispose();
        _cts?.Dispose();
    }
}