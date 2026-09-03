using System;
using System.Threading;

// Ejemplo 2
class SimpleThreadApp
{
    public static void WorkerThreadMethod()
    {   //==========================
        int milliseconds = 2000;
        Thread.Sleep(milliseconds);
        //==========================
        Console.WriteLine("[WorkerThreadMethod] Worker "+"thread started");
    }

    public static void Main()
    {
        ThreadStart worker = new ThreadStart(WorkerThreadMethod);
        Console.WriteLine("[Main] Creating worker thread");
        Thread t = new Thread(worker);
        t.Start();
        Console.WriteLine("[Main] Have requested the start of worker thread");
        Console.ReadKey();
    }
}