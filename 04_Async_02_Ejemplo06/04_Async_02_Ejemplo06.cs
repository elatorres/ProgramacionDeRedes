using System;
using System.Globalization;
using System.Threading.Tasks;

namespace AsyncSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Task<int> task1 = new Task<int>(() =>
            { 
                // Una tarea que hace sumatoria de 500
                int total = 0;
                for (int i = 0; i < 500; i++)
                {
                    total += i;
                }
                return total;
            });
            task1.Start();
            // cosas para hacer
            await Task.Delay(1000);
            int resultado1 = await task1;
            Console.WriteLine("El resultado1 es : {0}",resultado1);

            Task<int> task2 = new Task<int>(obj =>
            {
                // una tarea que recibe un parámetro y hace sumatoria de ese parámetro
                int total = 0;
                int max = (int)obj;
                for (int i = 0; i < max; i++)
                {
                    total += i;
                }
                return total;
            }, 300); // parámetro 300
            task2.Start();
            //  acá podríamos hacer otras cosas
            int resultado2 = await task2;
            Console.WriteLine("El resultado2 es : {0}",resultado2);
            await DoWork1();
            await DoWork2();
            Console.WriteLine("Fin del programa. Presione una tecla para terminar.");
            Console.ReadKey();
        }

        public static async Task DoWork1()
        {
            // sumo 4 mas 5 
            await Task.Delay(1000);
            // Creo una tarea completada con resultado de enteros a partir del entero que me devuelve...
            // en realidad el await no espera nada porque es un resultado listo...
            // int res1 = await Task.FromResult<int>(GetSum(4, 5));
            int res1 = Task.FromResult<int>(GetSum(4, 5)).Result; // extraigo el resultado ...
            Console.WriteLine("El resultado1 es : {0}",res1);
        }
        
        public static async Task DoWork2()
        {
            // otro sumo 4 mas 5. Creo uan función asincrónica 
            Func<int> funcion = new Func<int>(() => GetSum(4,5));
            // y la ejecuto en un hilo diferente mientras espero ...
            int res2 = await Task.Run<int>(funcion);
            Console.WriteLine("El resultado2 es : {0}",res2);
        }
        private static int GetSum(int a, int b)
        {
            return a + b;
        }
    }
}