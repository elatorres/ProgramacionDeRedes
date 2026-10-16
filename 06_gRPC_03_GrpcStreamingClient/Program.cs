#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using client;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading.Tasks;

namespace Client
{
    public class Program
    {
        private static readonly Random Random = new Random();

        static async Task Main(string[] args)
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:5000");
            var client = new Counter.CounterClient(channel);

            // Probamos 3 de los 4 tipos de llamadas grpc:

            // unary: (request-response)
            await UnaryCallExample(client);

            // Client-side streaming (stream de input -> un mensaje de respuesta del servidor)
            await ClientStreamingCallExample(client);

            // Server-side streaming (un mensaje de input del cliente -> stream de respuesta del servidor)
            await ServerStreamingCallExample(client);

            // Cuarto tipo: Streaming bi-direccional 

            Console.WriteLine("Shutting down");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task UnaryCallExample(Counter.CounterClient client)
        {
            var reply = await client.IncrementCountAsync(new Empty());
            Console.WriteLine("Count: " + reply.Count);
        }

        private static async Task ClientStreamingCallExample(Counter.CounterClient client)
        {
            using var call = client.AccumulateCount();
            for (var i = 0; i < 3; i++)
            {
                var count = Random.Next(5);
                Console.WriteLine($"Accumulating with {count}");
                await call.RequestStream.WriteAsync(new CounterRequest { Count = count });
                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            await call.RequestStream.CompleteAsync();

            var response = await call;
            Console.WriteLine($"Count: {response.Count}");
        }

        private static async Task ServerStreamingCallExample(Counter.CounterClient client)
        {
            using var call = client.Countdown(new Empty());

            await foreach (var message in call.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"Countdown: {message.Count}");
            }
        }
    }
}
