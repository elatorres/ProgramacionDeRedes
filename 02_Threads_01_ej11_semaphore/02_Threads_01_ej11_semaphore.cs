using System;
using System.Threading;
// Ejemplo 11
class ejemplo
{
    private static Thread[] threads = new Thread[10];
    private static Semaphore sem = new Semaphore(3, 3);

    static void C_sharpcorner()
    {
        Console.WriteLine("{0} is waiting in line ...", Thread.CurrentThread.Name);
        sem.WaitOne();
        Console.WriteLine("{0} enters the zone!", Thread.CurrentThread.Name);
        Thread.Sleep(300);
        Console.WriteLine("   {0} is leaving the zone!", Thread.CurrentThread.Name);
        sem.Release();
    }

    static int Main(string[] args)
    {
        for (int i = 0; i < threads.Length; ++i)
        {
            threads[i] = new Thread(C_sharpcorner);
            threads[i].Name = "thread_" + i;
            threads[i].Start();
        }
        
        foreach (var t in threads) t.Join();  // Wait for all ...
        Console.WriteLine("Press any key...");
        Console.ReadKey();
        return 0;
    }
}