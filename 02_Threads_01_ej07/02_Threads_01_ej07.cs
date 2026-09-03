using System;
using System.Threading;

class Database
{
    public void SaveData(string text)
    {
        Monitor.Enter(this);
        // SaveData in
        Console.WriteLine("Database.SaveData - Iniciado.");
        
        Console.WriteLine("Database.SaveData - Ejecutándose.");
        for (int i = 0; i < 100; i++)
        {
            Thread.Sleep(10);
            Console.Write(text);
            // if (i == 40) { throw new Exception(); } // <= UNCOMMENT!!

        }
        Console.WriteLine("\nDatabase.SaveData - Finalizado.");
        // SaveData out
        Monitor.Exit(this);
        Console.WriteLine("Database.SaveData - Terminado.");
    }

    class ThreadMonitor2App
    {
        public static Database db = new Database();

        public static void WorkerThreadMethod1()
        {
            Console.WriteLine("WorkerThreadMethod #1 - Iniciado.");
            Console.WriteLine("WorkerThreadMethod #1 - Invocando Database.SaveData.");
            db.SaveData("-");
            Console.WriteLine("WorkerThreadMethod #1 - Retornando desde Output.");
        }
        
        public static void WorkerThreadMethod2()
        {
            Console.WriteLine("WorkerThreadMethod #2 - Iniciado.");
            Console.WriteLine("WorkerThreadMethod #2 - Invocando Database.SaveData.");
            db.SaveData("0");
            Console.WriteLine("WorkerThreadMethod #2 - Retornando desde Output.");
        }

        public static int Main()
        {
            ThreadStart worker1 = new ThreadStart(WorkerThreadMethod1);
            ThreadStart worker2 = new ThreadStart(WorkerThreadMethod2);
            Console.WriteLine("Principal - Creado hilos de ejecución secundarios.");
            Thread t1 = new Thread(worker1);
            Thread t2 = new Thread(worker2);
            t1.Start();
            t2.Start();
            return 0;
        }
    }
}