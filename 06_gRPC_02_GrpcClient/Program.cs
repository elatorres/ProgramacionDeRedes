using greeter_namespace;
using Grpc.Net.Client;
// install 
// Google.Protobuf
// Grpc.AspNetCore
// Grpc.Net.Client
// Grpc.Tools
namespace GrpcClient
{
    internal class Client
    {
        static async Task Main(string[] args)
        {

            // En caso que usen mac y tengan problemas con httpS:
            //https://learn.microsoft.com/en-us/aspnet/core/grpc/troubleshoot?view=aspnetcore-6.0#call-insecure-grpc-services-with-net-core-client 
            // A partir de .NET 5 en el cliente solo tienen que conectarse al servicio en el puerto correspondiente a http:// (ver launSettings.json)

            Console.WriteLine("Creando gRPC Client...");

            // The port number must match the port of the gRPC server.
            // si desactivo el TSL tengo que usar el http en vez de https (solo si https me da problemas en mac)
            
            // ====================== FALTA EL TRY CATCH ===========================
            // Se conecta al servidor creando un canal gRPC
            using var channel = GrpcChannel.ForAddress("http://localhost:5035");
            // crea una instancia  de Greeter a través del canal.
            var client = new Greeter.GreeterClient(channel);
            
            // Hace request básica y deja la respuesta en reply ...  
            var reply = await client.SayHelloAsync(
                              new HelloRequest { Name = "Enrique" });
            // Imprime la respuesta ...
            Console.WriteLine("Greeting: " + reply.Message);
            
            // Suma avisamos:
            await Console.Out.WriteLineAsync("Sumando 5 + 4");
            // Llama a AddNUmber Async con los parámetros y devuelve en resultado
            NumbersReturn resultado = await client.AddNumbersAsync(new NumbersRequest
            {
                NumeroA = 4,
                NumeroB = 5,
            });
            Console.WriteLine("Total: " + resultado.Suma);

            // Lista de Usuario: Manda la solicitud e GetUserList en el servidor y devuelve en res.
            UserListReply res = await client.GetUserListAsync(new UserListRequest { });
            // Recorre la lista y extrae lso nombres.
            foreach (User user in res.Users)
            {
                await Console.Out.WriteLineAsync($"{user.Name} tiene {user.Age} años");
            }
            // ==========================================================================
            // Informa y termina ...
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }
    }
}