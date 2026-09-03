using System;
using System.Threading.Tasks;

class Program
{
    static async Task<string> hagoUnaCosaTask()
    {
        // Simulo trabajo asicrónico
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(1000); // La tarea trabaja 10 segundos
            Console.WriteLine("\t[hagoUnaCosaTask] {0}", i);
        }   // Y al final devuelve una cadena de texto
        return "[ hagoUnaCosaTask ]";
    }

    static async Task<string> hagoOtraCosaTask()
    {
        // Simulo trabajo asicrónico
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(1000); // Esta otra también hace trabajo asincrónico
            Console.WriteLine("\t\t[hagoOtraCosaTask] {0}", i);
        }        // Y al final devuelve otra cadena de texto 
        return "[ hagoOtraCosaTask ]";
    }
    static async Task Main(string[] args)  // Main también asincrónico
    {
        // Inicio ambas tareas. Nótese que no hay await en ninguno de ellos.
        // Task1 y Task2 son promesas a futuro del tipo Task<T>
        Task<string> task1 = hagoUnaCosaTask();
        Task<string> task2 = hagoOtraCosaTask();
        Console.WriteLine("[Main] Tareas comenzadas.");
        // Ahora Main hace sus cosas...
        Console.WriteLine("[Main] Hace sus otras cosas...");
        for (int i = 0; i < 5; i++)
        {
            // como hago await, espera a que el delay termine
            await Task.Delay(1000); // Simulamos trabajo asincrónico
            Console.WriteLine("[Main] {0}", i);
        } 
        // Me siento a esperar los resultados de las tareas
        Console.WriteLine("[Main] esperando por los resultados");
        // Main se bloquea y cuando la tarea termine,
        // devuelve el resultado y Main se desbloquea
        string result1 = await task1;
        // para cada tarea ...
        string result2 = await task2;
        // Concatenamos los resultados
        string finalMessage = result1 + " " + result2;
        // Y Main termina con la composición de ambos resultados...
        Console.WriteLine("[Main] resultado: <<{0}>>", finalMessage);
    }
}