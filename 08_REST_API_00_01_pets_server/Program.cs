// This is a simple but complete RESTful API built
// with ASP.NET Core 8, designed to manage a
// collection of Dog objects in-memory. 

// Create a factory for websites
var builder = WebApplication.CreateBuilder(args);

// Register services in the DI container
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer(); // For Swagger docs
builder.Services.AddSwaggerGen(); // Generates Swagger UI
builder.Services.AddControllers(); // Add the created Controllers, Our Dog Controller <-- This is required

// Build my app
var app = builder.Build();

// Add Swagger middleware if in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // enables Swagger middleware
    app.UseSwaggerUI(); // Shows Swagger UI in browser
}

// app.UseAuthorization(); // Optional if you add auth later

app.MapControllers(); // Needs AddControllers() above to work.  Maps controller routes

app.Run();  //Start the application

// ==============================================