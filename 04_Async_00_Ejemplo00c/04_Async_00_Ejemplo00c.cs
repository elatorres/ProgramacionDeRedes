using System;
using System.Threading;
namespace AsynchronousProgramming
{
    // tomado de https://dotnettutorials.net/lesson/async-and-await-operator-in-csharp/
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("El método Main comenzó......");
            
            UnMetodo();

            Console.WriteLine("El método Main terminó......");
            Console.WriteLine("\nPresione cualquier tecla para terminar");
            Console.ReadKey();
        }
        
        public async static void UnMetodo()
        {
            Console.WriteLine("  Un Método comenzó......");
            
            // Desde el funcionamiento es lo mismo, pero desde el punto
            // de vista de la ejecución no lo es...
            // Esto es un thread.sleep duerme el hilo principal por 4 segundos.
            Thread.Sleep(TimeSpan.FromSeconds(4));
            // await Task.Delay(TimeSpan.FromSeconds(4));
            Console.WriteLine("\n");
            Console.WriteLine("  Un Método terminó......");
        }
    }
}