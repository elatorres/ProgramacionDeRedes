using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using _09_websocket_00_razor_chat_client;
using ClientWS;
using ClientWS.Services;

// El template es un proyecto Web Blazor Simple
// En Pages/Home.razor linea 35 está la configuración de la dirección del servidor
// 35:        await WebSocketService.ConnectAsync("ws://localhost:5205/ws");
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<WebSocketService>();

await builder.Build().RunAsync();
