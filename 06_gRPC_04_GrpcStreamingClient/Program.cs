using Count;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading.Tasks;

namespace Client
{
    public class Client
    {
        private static readonly Random Random = new Random();

        static async Task Main(string[] args)
        {
            // Creates an HTTP/2 channel (necessary for gRPC). 
            // And connects to the gRPC server listening at port 5001.
            using var channel = GrpcChannel.ForAddress("http://localhost:5000");
            // CounterClient is the generated gRPC client class from the .proto file.
            var client = new Counter.CounterClient(channel);

            // Probamos 4 tipos de llamadas grpc:
            
            // Calls a method that takes a single empty request and gets back a single CounterReply.
            Console.WriteLine("unary: (request-response)"); 
            await UnaryCallExample(client);
            
            // This is like uploading multiple sensor readings and receiving one final computed result.
            Console.WriteLine("Client-side streaming (stream de input -> un mensaje de respuesta del servidor)");
            await ClientStreamingCallExample(client);
            
            // The client sends a request and the server sends a sequence of numbers 
            Console.WriteLine("Server-side streaming (un mensaje de input del cliente -> stream de respuesta del servidor)");
            await ServerStreamingCallExample(client);

            Console.WriteLine("Streaming bi-direccional (stream de ambos lados)"); 
            await BidirectionalStreamingCallExample(client);
            
            Console.WriteLine("Shutting down");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task UnaryCallExample(Counter.CounterClient client)
        {
            Console.WriteLine("Unary Call Example");
            // This is a one message in, one out request.
            var reply = await client.IncrementCountAsync(new Empty());
            Console.WriteLine("Count: " + reply.Count);
        }

        private static async Task ClientStreamingCallExample(Counter.CounterClient client)
        {
            Console.WriteLine("Client Streaming Call Example");
            // client.AccumulateCount() opens a stream to send multiple CounterRequest messages.
            using var call = client.AccumulateCount();
            // The client sends a stream of random numbers
            for (var i = 0; i < 3; i++)
            {
                var count = Random.Next(5);
                Console.WriteLine($"Accumulating with {count}");
                // write ConterRequests through the channel stream
                await call.RequestStream.WriteAsync(new CounterRequest { Count = count });
                // simulate work
                await Task.Delay(TimeSpan.FromSeconds(2));
            } // The client writes three messages (with small random counts),
            
            // then closes the stream. Se termina el streaming
            await call.RequestStream.CompleteAsync();
            
            // await call waits for the server’s aggregated response.
            var response = await call;
            Console.WriteLine($"Count: {response.Count}");
        }

        private static async Task ServerStreamingCallExample(Counter.CounterClient client)
        {
            Console.WriteLine("Server Streaming Call Example");
            // Sends one empty request.
            using var call = client.Countdown(new Empty());
            
            // The server sends a sequence of CounterReply messages (10,9,8,7,...).
            await foreach (var message in call.ResponseStream.ReadAllAsync())
            {
                // The client reads each message asynchronously as it arrives.
                Console.WriteLine($"Countdown: {message.Count}");
                // This is like subscribing to a notification feed.
            }
            // triggered when sender closed the stream
            Console.WriteLine("Stream ended.");
        }
        
        private static async Task BidirectionalStreamingCallExample(Counter.CounterClient client)
        {
            Console.WriteLine("Bidirectional Streaming Call Example");
            // It uses two asynchronous tasks, one for reading server messages
            // and one for sending user input.
            using var call = client.LiveCount();

            Console.WriteLine("Starting background task to receive messages");
            var readTask = Task.Run(async () =>
            {
            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"Live Count: {response.Count}");
            }
            // triggered when sender closed the stream
            Console.WriteLine("Stream ended.");
            });

            while (true)
            {
                Console.WriteLine("Enter a number to add (or press Enter to exit): ");
                var input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("Exiting...");
                    break;
                }

                if (int.TryParse(input, out var count))
                {
                    Console.WriteLine($"Adding {count}");
                    await call.RequestStream.WriteAsync(new CounterRequest { Count = count });
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a valid number.");
                }
            }

            await call.RequestStream.CompleteAsync(); // se termina el streaming

            // Wait briefly for server to process and finish stream, or force cancel
            var completedTask = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromSeconds(2)));
            if (completedTask != readTask)
            {
                // Server did not close its stream, force cleanup
                call.Dispose();
            }
        }
    }
}
