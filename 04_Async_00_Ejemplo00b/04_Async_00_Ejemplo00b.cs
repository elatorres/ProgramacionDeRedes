using System;
using System.Threading;
namespace AsynchronousProgramming
{
    // tomado de https://dotnettutorials.net/lesson/async-and-await-operator-in-csharp/
    internal class Program
    {
        // static void Main(string[] args)
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
            await Task.Delay(TimeSpan.FromSeconds(4));
            Console.WriteLine("\n");
            Console.WriteLine("  Un Método terminó......");
        }
    }
}