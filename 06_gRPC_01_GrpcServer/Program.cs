using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using GrpcMultiplication;

var builder = WebApplication.CreateBuilder(args);

// Configurar Kestrel para HTTP/2
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(50051, o => o.Protocols = HttpProtocols.Http2);
});

// Registrar los servicios de gRPC al constructor/fábrica 
builder.Services.AddGrpc();

var app = builder.Build();

// Registrar el servicio de calculadora
app.MapGrpcService<CalculatorService>();

System.Console.WriteLine(">>>>>>>>>>> gRPC Server escuchando en  http://localhost:50051");
await app.RunAsync();