using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

// install 
// Google.Protobuf
// Grpc.AspNetCore
// Grpc.Net.Client
// Grpc.Tools
namespace GrpcServer
{
    public class Server
    {
        public static void Main(string[] args)
        {
            // declaramos el constructor de aplicaciones web   
            var builder = WebApplication.CreateBuilder(args);

            // Additional configuration is required to successfully run gRPC on macOS.
            // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

            // En caso que usen mac y tengan problemas con https:
            //https://learn.microsoft.com/en-us/aspnet/core/grpc/troubleshoot?view=aspnetcore-6.0#call-insecure-grpc-services-with-net-core-client 
            // en el servidor tienen que agregar la siguiente opcion y usar el puerto de http:// (ver launSettings.json)
            
            // Agregamos configuración del servicio web que queremos al servidor Kestrel de ASP.NET
            builder.WebHost.ConfigureKestrel(options =>
            {
                // Setup a HTTP/2 endpoint without TLS (no recomendable en producción).
                // Escucha el 5035 en http v2.
                options.ListenLocalhost(5035, o => o.Protocols = HttpProtocols.Http2);
            });

            // Add gRPC services to the container.
            builder.Services.AddGrpc();
            
            // Construir el servidor web
            var app = builder.Build();

            // Configure the HTTP request pipeline. Mapear el servicio
            // gRPC que va a atender las solicitudes entrantes.
            app.MapGrpcService<GreeterService>();
            // Avisarle a los que se conecten con navegador que esto no es para ellos.
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            app.Run(); // bloqueante
            // no se va a ejecutar hasta que el app.Run() termine
        } // fin Main
    }
}