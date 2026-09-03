using System.Threading;
using System.Threading.Tasks;
namespace AsyncSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var task = new Task(() =>
            {

                Thread.Sleep(1000); // Bloquea el hilo. ¡No haga esto en su proyecto!
                Console.WriteLine("La tarea interna");
            });
            task.Start();
            Console.WriteLine("Ha terminado"); // termina el main y termina el proceso.
            Console.ReadLine();
        }
    }
}