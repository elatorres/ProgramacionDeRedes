using System;
using System.Threading.Tasks;
using Grpc.Net.Client;

//#=============================================================================#
//#  El proyecto 06_gRPC_00_GrpcService es un servidor gRPC que implementa el   #
//#  proyecto Greeter que se genera por defecto al inicializar un proyecto de   #
//#  servidor gRPC. El proyecto 06_gRPC_00_GrpcServiceClient genera una llamada #
//#  al servicio Greeter.                                                       #
//#=============================================================================#
namespace Cliente_gRPC
{
    class GrpcClient
    {
        static async Task Main(string[] args)
        {
            // The port number(5000) must match the port of the gRPC server.
            // using var channel = GrpcChannel.ForAddress("https://localhost:5000"); // try https
            var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            // Create a communication channel
            using var channel = GrpcChannel.ForAddress("http://localhost:5000", new GrpcChannelOptions
            {
                HttpHandler = httpHandler
            });
            // Connect the Greeter Service to the channel
            var client = new Greeter.GreeterClient(channel);
            
            // Asks for a name
            Console.WriteLine("Ingrese su nombre:");
            String nombre = Console.ReadLine();
            // Invoke the remote procedure SayHello with the name, and wait for the answer
            var reply = await client.SayHelloAsync(
                new HelloRequest { Name = nombre });
            // Show the result
            Console.WriteLine("Greeting: " + reply.Message);
            
            // wait for any key to be pressed
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}