namespace AsyncSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // creo una tarea que devuelve diez
            Task<int> task = Task.Run(() =>
            {
                // siempre termina bien
                return 10;
            });
            // task Continuations nos permiten reaccionar según como haya terminado la tarea.
            // Esta continuación sólo corre si la tarea es cancelada...
            task.ContinueWith((i) =>
            {
                Console.WriteLine("tarea Cancelada");
            }, TaskContinuationOptions.OnlyOnCanceled);
            // Esta continuación sólo corre si la tarea termina con una excepción...
            task.ContinueWith((i) =>
            {
                Console.WriteLine("tarea Fallada");
            }, TaskContinuationOptions.OnlyOnFaulted);
            // Esta continuación sólo se ejecuta si la tarea termina exitosamente...
            var completedTask = task.ContinueWith((i) =>
            {
                Console.WriteLine("tarea Completada");
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
            // Aquí el Main espera a que la tarea termine ...
            completedTask.Wait();
            // ... y termina.
            Console.ReadKey();
        }
    }
}