using System;
using System.Threading.Tasks; 

namespace Async_Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Las tareas se inician inmediatamente
            var cosa = HacerUnaCosa(10);  //  <<<<<<<< Tarea
            var otraCosa = HacerOtraMas(10);  //  <<<<<<<< Tarea
            
            int resultado = 1; // Inicializamos a 1 para la productoria
            // productoria de 10
            for (int i = 1; i <= 10; i++)
            {
                await Task.Delay(100); // nos aseguramos que espere
                resultado = resultado * i;
                Console.WriteLine("Main i({0})={1}", i, resultado);
            }

            // esperar por las otras tareas, si no terminaron todavía.
            int resultadoHOC = await cosa;
            int resultadoHOM = await otraCosa;

            Console.WriteLine($"Resultado final de Main: {resultado}");
            Console.WriteLine($"HacerUnaCosa Resultado: {resultadoHOC}");
            Console.WriteLine($"HacerOtraMas Resultado: {resultadoHOM}");
            Console.WriteLine("\nPresione cualquier tecla para terminar.");
            Console.ReadKey();
        }
        
        static async Task<int> HacerUnaCosa(int number)
        {
            // calcula la Productoria de number lentamente
            var taskOtra = HacerOtraCosa(number);  //  <<<<<<<< Tarea
            
            // calcula la sumatoria de number lentamente
            int sumatoria = 0;
            for (int i = 1; i <= number; i++)
            {
                await Task.Delay(100);
                sumatoria += i;
                Console.WriteLine("   H1C i({0})={1}", i, sumatoria);
            }

            Console.WriteLine("HacerUnaCosa esperando por HacerOtraCosa");
            int resOtra = await taskOtra; // Esperamos por el resultado si no terminó aún.
            return resOtra + sumatoria;
        }

        static async Task<int> HacerOtraCosa(int number)
        {
            // calcula la Productoria de number lentamente
            int resultado = 1; // Initialize to 1
            for (int i = 1; i <= number; i++)
            {
                await Task.Delay(100);
                resultado = resultado * i;
                Console.WriteLine("      HOC i({0})={1}", i, resultado);
            }
            return resultado;
        }

        static async Task<int> HacerOtraMas(int number)
        {
            // calcula la Productoria de number lentamente
            int resultado = 1; // Inicializar a 1
            for (int i = 1; i <= number; i++)
            {
                await Task.Delay(100);
                resultado = resultado * i;
                Console.WriteLine("         HOM i({0})={1}", i, resultado);
            }
            return resultado;
        }
    }    
}