using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 04_Async_00_Ejemplo03_parallel00 
class Program
{
    static void Main(string[] args)
    {
        // defino una lista de números para calcular sus cuadrados concurrentemente
        List<int> numbers = new List<int>();

        // Lleno la lista con números del 1 al 10
        for (int i = 1; i <= 10; i++)
        {
            numbers.Add(i);
        }

        Console.WriteLine("Comenzando el procesamiento paralelo...\n");

        // Usamos Parallel.ForEach para procesar cada numero (for each 'numbers')
        // le pasamos la función lambda a ejecutar, pero podría ser otra cosa.
        Parallel.ForEach(numbers, number =>
        {
            // Cada invocación se ejecuta en paralelo
            int square = number * number;
            Console.WriteLine($"Número: {number}, Cuadrado: {square}, Thread Id: {Environment.CurrentManagedThreadId}");
        });

        Console.WriteLine("\n¡Procesamiento completado!");
    }
}