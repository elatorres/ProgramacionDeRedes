using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==> Tarea que se completa:");
            // Obviamente devolver un número no puede tener un error
            var completedTask = Task.Run(() => 10);
            // declaramos las continuaciones con las diferentes opciones
            completedTask.ContinueWith((t) => Console.WriteLine("✅ tarea Completada"),
                TaskContinuationOptions.OnlyOnRanToCompletion);
            completedTask.ContinueWith((t) => Console.WriteLine("❌ tarea Cancelada"),
                TaskContinuationOptions.OnlyOnCanceled);
            completedTask.ContinueWith((t) => Console.WriteLine("💥 tarea Fallada"),
                TaskContinuationOptions.OnlyOnFaulted);
            // Lo correcto es capturar la excepción.
            // ¡La capturo en la promesa!
            try
            {
                // y el hilo principal del Main espera a que termine
                completedTask.Wait(); 
            }
            catch { } // capturar la excepción y seguir... pero esta nunca va a fallar ...

            Console.WriteLine("\n==> Tarea que lanza una excepción:");
            // Esta sólo puede fallar porque sólo tiene una excepción ....
            var faultedTask = Task.Run(() =>
            {
                throw new InvalidOperationException("Error simulado.");
            });
            // declaramos las continuaciones con las diferentes opciones
            faultedTask.ContinueWith((t) => Console.WriteLine("✅ tarea Completada"),
                TaskContinuationOptions.OnlyOnRanToCompletion);
            faultedTask.ContinueWith((t) => Console.WriteLine("❌ tarea Cancelada"),
                TaskContinuationOptions.OnlyOnCanceled);
            faultedTask.ContinueWith((t) => Console.WriteLine("💥 tarea Fallada"),
                TaskContinuationOptions.OnlyOnFaulted);
            try
            {
                faultedTask.Wait();
            }
            catch { } // capturar la excepción y seguir...

            Console.WriteLine("\n==> Tarea que es cancelada:");
            // A esta la vamos a cancelar a prepo...
            // generamos un cancellationToken, después volvemos sobre esto....
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            // generamos la tarea...
            var canceledTask = Task.Run(() =>
            {
                // Si hay una cancellationToken entonces generar una excpción y salir...
                token.ThrowIfCancellationRequested();
                return 10;
            }, token); // recibe el cancellationToken
            // declaramos las continuaciones con las diferentes opciones
            canceledTask.ContinueWith((t) => Console.WriteLine("✅ tarea Completada"),
                TaskContinuationOptions.OnlyOnRanToCompletion);
            canceledTask.ContinueWith((t) => Console.WriteLine("❌ tarea Cancelada"),
                TaskContinuationOptions.OnlyOnCanceled);
            canceledTask.ContinueWith((t) => Console.WriteLine("💥 tarea Fallada"),
                TaskContinuationOptions.OnlyOnFaulted);
            // solicitar la cancelación de la tarea
            cts.Cancel();
            // esperamos a que termine la tarea
            try
            {
                canceledTask.Wait();
            }
            catch { } // consumir la excepción de la cancelación

            Console.WriteLine("\nPresione una tecla para salir...");
            Console.ReadKey();
        }
    }
}
