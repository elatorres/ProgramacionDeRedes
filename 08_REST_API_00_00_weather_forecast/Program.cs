var builder = WebApplication.CreateBuilder(args);

// Agrego servicios al constructor. El controlador del WeatherForecast
builder.Services.AddControllers();
// Agrego la funcionalidad de atender a los url y redirigirlo a los servicios
builder.Services.AddEndpointsApiExplorer();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Agrego el servicio Swagger, un consumidor web de APIREST
// https://localhost:7122/swagger/index.html
builder.Services.AddSwaggerGen();  

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger(); // Con swagger
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection(); 
            
// Autorizar a usuarios a acceder a los recursos
app.UseAuthorization();
            
// agregar los controladores
app.MapControllers();
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");


Console.WriteLine("Connect to http://localhost:5000/swagger/index.html");
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}