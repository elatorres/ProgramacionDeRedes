using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;

namespace GrpcAuthExample
{
    class Program
    {
        // Server URL
        const string Address = "http://localhost:5000";

        static async Task Main(string[] args)
        {
            // Enable HTTP/2 for unencrypted HTTP connections (if using http:// instead of https://)
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Create a modern gRPC channel
            using var channel = GrpcChannel.ForAddress(Address);
            
            // Initialize the gRPC client
            var client = new AuthService.AuthServiceClient(channel);

            // 1. Authenticate to retrieve a Bearer token
            var authResponse = await client.AuthenticateAsync(new AuthRequest
            {
                Username = "user",
                Password = "pass"
            });

            if (!string.IsNullOrEmpty(authResponse.Token))
            { 
                Console.WriteLine($"Authentication successful: {authResponse.Message}");
                Console.WriteLine($"Token: {authResponse.Token}");

                // 2. Standard Best Practice: Pass JWT/Token in HTTP Metadata Headers
                // (Instead of passing Token in every message payload)
                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {authResponse.Token}" }
                };

                // Request general data with Authorization header
                var dataResponse = await client.GetDataAsync(
                    new DataRequest { Token = authResponse.Token }, 
                    headers: headers
                );

                Console.WriteLine($"Server response: {dataResponse.Data}");

                // Request filtered data
                var filteredDataResponse = await client.GetFilteredDataAsync(
                    new FilteredDataRequest
                    {
                        Token = authResponse.Token,
                        FilterString = "example",
                        FilterNumber = 42
                    }, 
                    headers: headers
                );

                Console.WriteLine($"Filtered data response: {filteredDataResponse.Result}");
            }
            else
            {
                Console.WriteLine($"Authentication failed: {authResponse.Message}");
            }

            // Channel disposes automatically via 'using var channel'
        }
    }
}