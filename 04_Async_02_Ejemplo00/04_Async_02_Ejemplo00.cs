namespace AsyncSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // podemos declarar así ....
            /*
            var task = new Task(() =>
            {
                Console.WriteLine("Hello World!");
            });
            task.Start(); // <-- Iniciamos la tarea. ¿Qué pasa si no la iniciamos?¿Si esto falta?
            // otra código para ejecutar...
            await task; // <-- Espera a que termine la tarea. 
            */
            // O lo que es lo mismo ...
            
            await Task.Run(() =>  // <-- Corre la tarea y espera a que termine.
            {
                Console.WriteLine("Hello World!");
            });
            Console.WriteLine("¡Ha terminado!");
        }
    }
}