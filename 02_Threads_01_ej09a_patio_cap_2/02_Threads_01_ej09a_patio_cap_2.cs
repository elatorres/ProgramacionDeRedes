﻿﻿using System;
using System.Threading;

 // Patio de capacidad dos
class LimitedAccess
{
    // generador de tiempo aleatorio
    private static readonly Random waitTime = new Random();
    // mi objeto de exclusión mutua
    private static readonly object lockObj = new object();
    // cantidad de hilos
    private static int runningThreads = 0;
    // Cantidad máxima de hilos dentro de la región con acceso limitado
    private const int MAX_THREADS = 2; // Máximo dos hilos a la vez
    // https://dotnettutorials.net/lesson/multithreading-using-monitor/
    
    // mi zona de acceso limitado
    public void CriticalMethod(string threadName)
    {
        lock (lockObj)  // asegura que sólo un hilo a la vez puede modificar la variable runningThreads.
        {
            while (runningThreads >= MAX_THREADS) // si ya esta colmada la capacidad entonces seguir bloqueando 
            {
                Console.WriteLine($"[ {threadName} ] Esperando por un cupo disponible...");
                Monitor.Wait(lockObj); // Esperar bloqueado hasta que un slot esté disponible.
                // Cuando se invoca el método Wait de la clase Monitor, se libera el bloqueo del objeto lockObj
                // y se bloquea el hilo actual.
                // vuelva a ser liberado por un Pulse en el mismo objeto.
                // Y vuelve a evaluar la condición. Cuando la condición no se cumple sale del while
            }
            runningThreads++; // Incrementar la cuenta de hilos en la región
        }  // Fin de la exclusión mutua

        try
        {
            // Hacer lo que se tenga que hacer dentro de la región con acceso restringido...
            Console.WriteLine($"[ {threadName} ] Ingresando a la Región Crítica.");
            Thread.Sleep(waitTime.Next(0, 2100)); // Simulamos trabajo
            Console.WriteLine($"[ {threadName} ] Saliendo de la Región Crítica.");
        }
        finally  // Siempre desbloquear si hay excepción ...
        {
            lock (lockObj) // asegura que sólo un hilo a la vez puede modificar la variable runningThreads.
            {
                runningThreads--; // Decrementamos la cuenta de hilos en la región restringida
                // Cuando se invoca el método Pulse de la clase Monitor, el libera al prime hilo
                // que se encuentra en la cola de espera del objeto lockObj. Y el objeto
                // eventualmente comienza a ejecutar y entra en la región restringida 
                Monitor.Pulse(lockObj); // Notificamos al hilo en espera
            }  // Fin de la exclusión mutua
        }
    } // Fin de la Región Restringida
}  // Fin de la clase LimitedAccess

class Program
{
    static void Main()
    {
        // creamos nuestra Región de Acceso Restringido
        LimitedAccess obj = new LimitedAccess();

        // Creamos 4 hilos
        Thread t1 = new Thread(() => obj.CriticalMethod("Thread 1"));
        Thread t2 = new Thread(() => obj.CriticalMethod("Thread 2"));
        Thread t3 = new Thread(() => obj.CriticalMethod("Thread 3"));
        Thread t4 = new Thread(() => obj.CriticalMethod("Thread 4"));

        // Iniciamos los hilos
        t1.Start();
        t2.Start();
        t3.Start();
        t4.Start();

        // Esperamos que los hilos terminen
        t1.Join();
        t2.Join();
        t3.Join();
        t4.Join();

        Console.WriteLine("Todos los hilos han terminado.");
    }
}