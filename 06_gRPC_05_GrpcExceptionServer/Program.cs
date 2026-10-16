using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace GrpcDivisionExample
{
    public class Program
    {
        const int Port = 5000;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddGrpc();
            builder.Services.AddSingleton<CalculatorServiceImpl>();

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(Port, o => o.Protocols = HttpProtocols.Http2);
            });

            var app = builder.Build();

            app.MapGrpcService<CalculatorServiceImpl>();

            System.Console.WriteLine($"Calculator gRPC Server listening on http://localhost:{Port}...");
            app.Run();
        }
    }
}