using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers(); // Add the created Controllers

// Build my app
var app = builder.Build();

// Add Swagger middleware if in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // enables Swagger middleware
    app.UseSwaggerUI(); // Shows Swagger UI in browser
}
app.MapControllers(); // Needs AddControllers() above to work.  Maps controller routes
Console.WriteLine("Connect to  http://localhost:5173/swagger/index.html");
// Shutdown logic
var cts = new CancellationTokenSource();
_ = Task.Run(() => {
    Console.WriteLine("Type 'exit' to shut down...");
    while (Console.ReadLine()?.ToLower() != "exit") ;
    cts.Cancel();
});

app.Lifetime.ApplicationStopping.Register(() =>
    Console.WriteLine("Shutting down, waiting ongoing tasks..."));

await app.RunAsync(cts.Token);  // http://localhost:5169

Console.WriteLine("Exiting...");