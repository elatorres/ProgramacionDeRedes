using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Comenzando Parallel.For...\n");

        // Para los números desde 0 a 9 en paralelo
        Parallel.For(0, 10, i =>
        {
            Console.WriteLine($"Iteration {i} ejecutado por el hilo {Environment.CurrentManagedThreadId}");
        });

        Console.WriteLine("\n¡Parallel.For completado!");
    }
}