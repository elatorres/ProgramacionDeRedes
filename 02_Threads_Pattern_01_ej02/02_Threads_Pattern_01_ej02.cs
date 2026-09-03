namespace TaskPattern
{
    // PR_03_p90_TaskPatternEvent
    // Ejemplo 14
    class Program
    {
        // A partir de .NET 6 no es necesario usar un Main explícito
        static int Main(string[] args)
        {            
            // La firma tiene que coincidir con el delegado
            // public delegate void FinishTask(AbstrackTask abstractTask);
            static void PrintFinishedTask(AbstractTask abstractTask)
            {
                Console.WriteLine("[Main] Task Finished! task ---> "+ abstractTask);
            }

            Console.WriteLine("[Main] Starting Test!");
            var concreteTask = new ConcreteTask();
            // La tarea se va a ejecutar en paralelo
            concreteTask.Execute();
            
            // Le agregamos las funciones que se van a llamar cuando se termine de ejecutar la tarea de concreteTask.
            // Guarda una lista de funciones. Aquí ponemos 4 veces la misma pero podrían ser diferentes
            concreteTask.OnFinishTask += new FinishTask(PrintFinishedTask);
            concreteTask.OnFinishTask += new FinishTask(PrintFinishedTask);
            concreteTask.OnFinishTask += new FinishTask(PrintFinishedTask);
            concreteTask.OnFinishTask += new FinishTask(PrintFinishedTask);

            // El hilo principal se va a seguir ejecutando
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                Console.WriteLine("[Main] Running Code in Main: "+i);
            }
            
            //Esta tarea se va a ejecutar una vez que la tarea se termine de ejecutar.
            Console.WriteLine("[Main] End...");
            return 0;
        }
    }
}