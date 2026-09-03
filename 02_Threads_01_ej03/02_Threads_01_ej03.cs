using System;
using System.Threading;
// ejemplo 3
class ThreadSchedulerApp
{
    public static void WorkerThreadMethod1()
    {   
        Console.WriteLine("[WorkerThreadMethod1] Hilo de ejecución secundario iniciado");
        Console.WriteLine("[WorkerThreadMethod1] Hilo de ejecución secundario contando lentamente de 1 a 10");
        for (int i = 1; i < 11; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                Console.Write(".");
                int a;
                a = 15;
            }
            Console.Write("{0}",i);
        }
        Console.WriteLine("[WorkerThreadMethod1] Hilo de ejecución secundario finalizado");
    }

    public static void WorkerThreadMethod2()
    {   
        Console.WriteLine("[WorkerThreadMethod2] Hilo de ejecución secundario iniciado");
        Console.WriteLine("[WorkerThreadMethod2] Hilo de ejecución secundario contando lentamente de 11 a 20");
        for (int i = 11; i < 20; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                Console.Write("º");
                int a;
                a = 15;
            }
            Console.Write("{0}",i);
        }
        Console.WriteLine("[WorkerThreadMethod2] Hilo de ejecución secundario finalizado");
    }
    
    public static void Main()
    {
        ThreadStart worker1 = new ThreadStart(WorkerThreadMethod1);
        ThreadStart worker2 = new ThreadStart(WorkerThreadMethod2);
        Console.WriteLine("[Main] Creando hilos de ejecución secundarios");
        Thread t1 = new Thread(worker1);
        Thread t2 = new Thread(worker2);
        //t1.Priority = ThreadPriority.Highest;
        //t2.Priority = ThreadPriority.Lowest;
        t1.Start();
        t2.Start();
        Console.WriteLine("[Main] Ha solicitado el inicio de ambos hilos secundarios");
        // Console.ReadKey();
    }
}