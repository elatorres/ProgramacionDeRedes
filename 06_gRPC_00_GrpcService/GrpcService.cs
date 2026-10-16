using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

//#=============================================================================#
//#  El proyecto 06_gRPC_00_GrpcService es un servidor gRPC que implementa el   #
//#  proyecto Greeter que se genera por defecto al inicializar un proyecto de   #
//#  servidor gRPC. El proyecto 06_gRPC_00_GrpcServiceClient genera una llamada #
//#  al servicio Greeter.                                                       #
//#=============================================================================#
namespace Server_gRPC
{
    public class GrpcServer
    {
        public static async Task Main(string[] args)
        {
            // creamos un cancellation token source cts
            using var cts = new CancellationTokenSource();
            // Precisamos un servidor web 2.0 (sin TLS). Iniciamos un servidor kestrel
            // Create the host builder
            var host = CreateHostBuilder(args).Build();
            
            // Start a background task that listens for "exit"
            _ = Task.Run(() =>
            {
                while (true)
                {
                    Console.Write("> ");
                    var input = Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (input == "exit")
                    {
                        Console.WriteLine("Stopping server...");
                        cts.Cancel(); // signal cancellation
                        break;
                    }
                }
            });
            Console.WriteLine("gRPC server running on http://localhost:5000/");
            Console.WriteLine("Type 'exit' to stop the server.");

            // Run the server (until cancellation)
            await host.RunAsync(cts.Token);

            Console.WriteLine("Server stopped.");
        }
        
        // configura la aplicación (appsettings.json, variables de ambiente,
        // y parámetros por línea de comando - si se usan)
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // configurar el servidor kestrel
                    webBuilder.ConfigureKestrel(options =>
                    {
                        // Habilitar HTTP/2 (obligatorio para gRPC) sin TLS (h2c) en puerto 5000
                        options.ListenLocalhost(5000, listenOptions =>
                        {
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                        });
                    });
                    // lanzar el servidor
                    webBuilder.UseStartup<Startup>();
                });
    }
}