var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); // 1

var app = builder.Build();

WebSocketOptions wsOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(60), // Envia un ping cada 1 minuto. (default 2 minutos)
};

app.UseWebSockets(wsOptions);

app.MapControllers(); // 2

app.Run();
// El template es un proyecto Web API simple