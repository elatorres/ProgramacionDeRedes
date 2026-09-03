using System;
using System.Threading;

namespace TaskPattern
{
    class Program
    {
        // declaramos la tarea a ejecutar cuando se termine el hilo.
        static void PrintFinishedTask(AbstractTask abstractTask)
        {
            Console.WriteLine("[Main] Task Finished! task ---> " + abstractTask);
        }

        static int Main(string[] args)
        {
            Console.WriteLine("[Main] Starting Test!");
            // instanciamos un objeto de la clase concreta
            var concreteTask = new ConcreteTask();

            // Para que no haya ejecuciones duplicadas las borramos de la lista
            concreteTask.OnFinishTask -= PrintFinishedTask;
            // y luego agregamos una ...
            concreteTask.OnFinishTask += PrintFinishedTask;

            // Comenzamos la tarea
            concreteTask.Execute();

            // El hilo principal continúa la ejecución en paralelo
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                Console.WriteLine($"[Main] Running Code in Main: {i}");
            }

            // Nos aseguramos que la tarea termine antes de terminar el hilo principal.
            concreteTask.WaitForFinish();
            
            // terminamos...
            Console.WriteLine("[Main] End...");
            return 0;
        }
    }
}