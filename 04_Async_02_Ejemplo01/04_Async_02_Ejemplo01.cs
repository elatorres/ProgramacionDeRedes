namespace AsyncSample
{
    class Program
    {
        // Warning CS1998 : This async method lacks 'await' operators and will run synchronously.
        static async Task Main(string[] args)
        {
            var task = new Task(() =>
            {
                Thread.Sleep(1000);
                Console.WriteLine("La tarea interna");  // <-- ¿Qué pasó?
            });
            task.Start();
            await task;
            Console.WriteLine("¡Ha terminado!");
            /* Console.WriteLine("Presione cualquier tecla para terminar");
            Console.ReadKey(); */ 
        }
    }
}