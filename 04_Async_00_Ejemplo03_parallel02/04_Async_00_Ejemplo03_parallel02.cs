using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Comenzando Parallel.Invoke...\n");

        Parallel.Invoke(
            () => DoTask("Tarea A", 1000),
            () => DoTask("Tarea B", 1500),
            () => DoTask("Tarea C", 500)
        );

        Console.WriteLine("\nParallel.Invoke completado!");
    }

    static void DoTask(string taskName, int delayMilliseconds)
    {
        Console.WriteLine($"{taskName} comenzada en el hilo {Environment.CurrentManagedThreadId}");
        Task.Delay(delayMilliseconds).Wait(); // Simulamos algo de trabajo
        Console.WriteLine($"{taskName} terminada en hilo {Environment.CurrentManagedThreadId}");
    }
}