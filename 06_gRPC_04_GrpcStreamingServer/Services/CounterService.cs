using Count;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Server
{
    
    public class CounterService : Counter.CounterBase
    {
        private readonly ILogger _logger;
        private readonly IncrementingCounter _counter;

        public CounterService(IncrementingCounter counter, ILoggerFactory loggerFactory)
        {
            _counter = counter;
            _logger = loggerFactory.CreateLogger<CounterService>();
        }

        public override Task<CounterReply> IncrementCount(Empty request, ServerCallContext context)
        {
            _logger.LogInformation("Incrementing count by 1");
            _counter.Increment(1);

            return Task.FromResult(new CounterReply { Count = _counter.Count });
        }

        public override async Task<CounterReply> AccumulateCount(IAsyncStreamReader<CounterRequest> requestStream, ServerCallContext context)
        {
            await foreach (var message in requestStream.ReadAllAsync())
            {
                _logger.LogInformation($"Incrementing count by {message.Count}");

                _counter.Increment(message.Count);
            }

            return new CounterReply { Count = _counter.Count };
        }

        public override async Task Countdown(Empty request, IServerStreamWriter<CounterReply> responseStream, ServerCallContext context)
        {
            for (var i = _counter.Count; i >= 0; i--)
            {
                await responseStream.WriteAsync(new CounterReply { Count = i });
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        public override async Task LiveCount(
            IAsyncStreamReader<CounterRequest> requestStream, 
            IServerStreamWriter<CounterReply> responseStream, 
            ServerCallContext context)
        {
            // Task 1: Read client requests and increment the counter
            var readerTask = Task.Run(async () =>
            {
                await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
                {
                    _logger.LogInformation($"Incrementing count by {message.Count}");
                    _counter.Increment(message.Count);
                }
            }, context.CancellationToken);

            // Task 2: Reactively stream counter updates back to the client via Channel
            var writerTask = Task.Run(async () =>
            {
                // Send initial current value immediately
                await responseStream.WriteAsync(new CounterReply { Count = _counter.Count });

                // Stream updates reactively whenever _counter.Increment() writes to the channel
                await foreach (var count in _counter.ReadCountUpdatesAsync(context.CancellationToken))
                {
                    await responseStream.WriteAsync(new CounterReply { Count = count });
                }
            }, context.CancellationToken);

            try
            {
                // When client calls CompleteAsync(), readerTask finishes, but writerTask stays open until call cancels
                await Task.WhenAny(readerTask, writerTask);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("LiveCount call completed or canceled.");
            }
        }

    }
}
