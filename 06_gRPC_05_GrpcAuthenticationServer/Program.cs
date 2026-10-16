using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GrpcAuthExample
{
    public class Program
    {
        const int Port = 5000;
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Add gRPC framework
            builder.Services.AddGrpc();

            // FIX: Register AuthServiceImpl as a Singleton so _tokens persists across requests
            builder.Services.AddSingleton<AuthServiceImpl>();

            // 2. Configure Kestrel to listen on HTTP/2 without TLS
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(Port, o => o.Protocols = HttpProtocols.Http2);
            });

            var app = builder.Build();

            // 3. Map your gRPC service
            app.MapGrpcService<AuthServiceImpl>();

            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

            System.Console.WriteLine($"Server listening on port {Port} (HTTP/2)...");
            app.Run();
        }
    }
}
