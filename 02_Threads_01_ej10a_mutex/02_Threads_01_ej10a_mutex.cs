﻿using System;
using System.Threading;

class Database
{
    // El mutex se usa para asegurar que sólo un hilo a la vez puede
    // ejecutar el método SaveData().
    private static Mutex mutex = new Mutex(false);
    
    public static void SaveData(string text)
    {
        mutex.WaitOne();  // Exclusión mutua
        try  // capturamos las excepciones <<==========
        {
            // Adentro de SaveData
            Console.WriteLine("Database.SaveData - Iniciado.");
            Console.WriteLine("Database.SaveData - Ejecutándose.");
            for (int i = 0; i < 100; i++)
            {
                // Trabajamos en SaveData
                Console.Write(text);
                if (i==40) throw new Exception("ERROR!");
            }

            Console.WriteLine("\nDatabase.SaveData - Finalizado.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("\nDatabase.SaveData - Error: {0}", ex.Message);
        }
        finally // y siempre desbloqueamos 
        {
            mutex.ReleaseMutex(); // termina exclusión mutua
        }
    } // Fin SaveData
} // FIn Database

class ThreadMutexApp
{
    //public static Database db = new Database();
    // Worker 1
    public static void WorkerThreadMethod1()
    {
        Console.WriteLine("WorkerThreadMethod #1 - Iniciado.");
        Console.WriteLine("WorkerThreadMethod #1 - Invocando Database.SaveData.");
        Database.SaveData("-");
        Console.WriteLine("WorkerThreadMethod #1 - Retornando desde Output.");
    }
    
    // worker 2
    public static void WorkerThreadMethod2()
    {
        Console.WriteLine("WorkerThreadMethod #2 - Iniciado.");
        Console.WriteLine("WorkerThreadMethod #2 - Invocando Database.SaveData.");
        Database.SaveData("0");
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
        t1.Join();
        t2.Join();  // Esperar a que los hilos terminen
        return 0;  // devolver todo bien
    } // Fin Main
} // Fin ThreadMutexApp
