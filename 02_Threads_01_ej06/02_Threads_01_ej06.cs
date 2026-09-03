using System;
using System.Threading;
// Ejemplo 6
internal class Database
{
    public void SaveData(string text)
    {
        Console.WriteLine("Database.SaveData - Iniciado.");

        Console.WriteLine("Database.SaveData - Ejecutándose.");
        for (var i = 0; i < 10; i++)
        {
            Thread.Sleep(10);
            Console.Write(text);
        }

        Console.WriteLine("\nDatabase.SaveData - Finalizado.");
    }
}

internal class ThreadMonitor1App
{
    public static Database db = new();

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
        var worker1 = new ThreadStart(WorkerThreadMethod1);
        var worker2 = new ThreadStart(WorkerThreadMethod2);
        Console.WriteLine("Principal - Creado hilos de ejecución secundarios.");
        var t1 = new Thread(worker1);
        var t2 = new Thread(worker2);
        t1.Start();
        t2.Start();
        return 0;
    }
}