using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;

namespace GrpcDivisionExample
{
    class Program
    {
        const string Address = "http://localhost:5000";

        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            using var channel = GrpcChannel.ForAddress(Address);
            var client = new CalculatorService.CalculatorServiceClient(channel);

            // Test 1: Valid Division
            await ExecuteDivisionAsync(client, 10, 2);

            Console.WriteLine();

            // Test 2: Division by Zero
            await ExecuteDivisionAsync(client, 10, 0);
        }

        private static async Task ExecuteDivisionAsync(
            CalculatorService.CalculatorServiceClient client, 
            double numerator, 
            double denominator)
        {
            Console.WriteLine($"[Client] Requesting {numerator} / {denominator}...");

            try
            {
                var response = await client.DivideAsync(new DivideRequest
                {
                    Numerator = numerator,
                    Denominator = denominator
                });

                Console.WriteLine($"[Client] Result: {response.Result}");
            }
            catch (RpcException ex)
            {
                // Exception Callback / Error Handling
                Console.WriteLine($"[gRPC Exception Caught]");
                Console.WriteLine($" -> Status Code: {ex.StatusCode}");
                Console.WriteLine($" -> Detail: {ex.Status.Detail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General Exception] {ex.Message}");
            }
        }
    }
}