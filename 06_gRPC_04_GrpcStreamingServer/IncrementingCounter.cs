using System.Threading;
using System.Threading.Channels;

namespace Server
{
    public class IncrementingCounter
    {
        private int _count;
        
        // Channel configured for single/multiple producers and multiple consumers
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = false
        });

        public int Count => Volatile.Read(ref _count);

        public void Increment(int amount)
        {
            // Atomically increment the internal counter with exclusion
            var newCount = Interlocked.Add(ref _count, amount);

            // Publish the update to the channel without blocking
            _channel.Writer.TryWrite(newCount);
        }

       
        // Reads counter updates as they are published.
        public IAsyncEnumerable<int> ReadCountUpdatesAsync(CancellationToken cancellationToken = default)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
