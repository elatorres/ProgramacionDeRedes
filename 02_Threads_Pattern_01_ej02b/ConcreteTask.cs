using System;
using System.Threading;

namespace TaskPattern
{
    public class ConcreteTask : AbstractTask
    {
        // implementa la clase abstracta en una concreta con la siguiente definición del proceso a ejecutar.
        protected override void Process()
        {
            Console.WriteLine("[Process] Starting Concrete task!");
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(100);
                Console.WriteLine($"[Process] Running task Iteration: {i}");
            }
            Console.WriteLine("[Process] Finishing Concrete task!");
        }
    }
}