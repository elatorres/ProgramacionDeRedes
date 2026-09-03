namespace TaskPattern
{
    class Program
    {
        static void PrintFinishedTask(AbstractTask abstractTask)
        {
            Console.WriteLine("[Main] Task Finished! task ---> " + abstractTask);
        }

        static int Main(string[] args)
        {
            Console.WriteLine("[Main] Starting Test!");

            var concreteTask = new ConcreteTask();

            // Remove existing event handlers to avoid duplicate calls
            concreteTask.OnFinishTask -= PrintFinishedTask;
            concreteTask.OnFinishTask += PrintFinishedTask;

            // Start the task asynchronously
            concreteTask.Execute();

            // Main thread continues running in parallel
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                Console.WriteLine($"[Main] Running Code in Main: {i}");
            }

            // Ensure the task finishes before exiting
            concreteTask.WaitForFinish();

            Console.WriteLine("[Main] End...");
            return 0;
        }
    }
}