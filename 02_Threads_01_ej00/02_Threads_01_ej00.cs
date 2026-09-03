using System;
using System.Threading;

// ejemplo 1
class SimpleThreadApp
{
    public static void WorkerThreadMethod()
    {
        Console.WriteLine("[WorkerThreadMethod] Worker "+"thread started");
        // Thread.Sleep(300);
        // Console.WriteLine("[WorkerThreadMethod] Worker "+"thread finished");
    }

    public static void Main()
    {
        // declaramos la función delegada
        ThreadStart worker = new ThreadStart(WorkerThreadMethod);
        Console.WriteLine("[main] Creating worker thread");
        Thread t = new Thread(worker);
        t.Start();
      
        Console.WriteLine("[Main] have requested the start of worker thread");
      
        // Console.WriteLine("ThreadState: {0}",t.ThreadState);
        // Thread.Sleep(500);
        // Console.WriteLine("ThreadState: {0}",t.ThreadState);
      
        Console.WriteLine("[Main] Press any key to end");
        Console.ReadKey();
    }
}