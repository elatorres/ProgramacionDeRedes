using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace GrpcAuthExample
{
    public class Program
    {
        const int Port = 5000; // Standard port for HTTPS gRPC

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Add gRPC framework and register service
            builder.Services.AddGrpc();
            builder.Services.AddSingleton<AuthServiceImpl>();

            // 2. Load the Certificate + Key from PEM files
            var certPath = Path.Combine(builder.Environment.ContentRootPath, "server.crt");
            var keyPath = Path.Combine(builder.Environment.ContentRootPath, "server.key");

            var serverCert = X509Certificate2.CreateFromPemFile(certPath, keyPath);

            // 3. Configure Kestrel for TLS/HTTPS
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(Port, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2; // Force HTTP/2 for gRPC
                    listenOptions.UseHttps(serverCert);          // Enable TLS with your certificate
                });
            });

            var app = builder.Build();

            app.MapGrpcService<AuthServiceImpl>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

            System.Console.WriteLine($"gRPC Server listening on https://localhost:{Port} with TLS...");
            app.Run();
        }
    }
}