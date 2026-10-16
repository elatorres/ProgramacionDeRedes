using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using GrpcMultiplication;

// Configurar el canal HTTP/2 apuntando al servidor
using var channel = GrpcChannel.ForAddress("http://localhost:50051");
var client = new Calculator.CalculatorClient(channel);

Console.WriteLine("--- Cliente de Multiplicación GRPC ---");

while (true)
{
    double num1 = await GetNumberFromUserAsync("Ingrese el primer número (o escriba 'exit' para salir): ");
    double num2 = await GetNumberFromUserAsync("Ingrese el segundo número: ");

    Console.WriteLine("[Cliente] Enviando números al servidor...");

    try
    {
        // LLamar al servidor
        var response = await client.MultiplyAsync(new MultiplyRequest 
        { 
            Number1 = num1, 
            Number2 = num2 
        });

        Console.WriteLine($"[Cliente] El resultado recibido del Servidor: {response.Result}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Cliente] Error de comunicación con el Servidor: {ex.Message}\n");
    }
}

// Helper para leer el mensaje del usuario desde la consola
async Task<double> GetNumberFromUserAsync(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        // Usando Task.Run para mantener a Console.ReadLine fuera del hilo principal (porque es bloqueante en thread)
        string input = (await Task.Run(() => Console.ReadLine())) ?? "";

        if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Closing client application...");
            Environment.Exit(0);
        }

        if (double.TryParse(input, out double result))
        {
            return result;
        }

        Console.WriteLine("Ingreso inválido. Por favor ingrese un número.");
    }
}