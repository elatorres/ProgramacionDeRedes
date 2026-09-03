namespace TaskPattern
{ 
    // Ejemplo 13 
    class Program
    {
        // A partir de .NET 6 no es necesario usar un Main explícito
        static int Main(string[] args)
        {
            Console.WriteLine("Starting Test!");
            var concreteTask = new ConcreteTask();
            // La tarea se va a ejecutar en paralelo
            concreteTask.Execute(PrintFinishedTask);
            
            // El hilo principal se va a seguir ejecutando
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                Console.WriteLine("Running Code in Main: "+i);
            }
            
            // Se bloquea el Main hasta que termine la tarea 
            concreteTask.WaitForFinish();
            
            //Esta tarea se va a ejecutar una vez que la tarea se termine de ejecutar.
            Console.WriteLine("End...");
            return 0;
        }
        
        // La firma tiene que coincidir con el delegado
        // public delegate void FinishTask (AbstractTask abstracktTask);
        static void PrintFinishedTask (AbstractTask abstracktTask)
        {
            Console.WriteLine("Task Finished! task ---> " + abstracktTask);
        }
    }
}