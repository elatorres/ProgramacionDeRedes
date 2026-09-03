﻿using System;
using System.ComponentModel;
using System.Threading;

namespace TaskPattern
{
    class Program
    {
        // evento para indicar al main que el worker terminó
        static ManualResetEvent doneEvent = new ManualResetEvent(false); // Event to signal completion
        
        // el worker ...
        static void WorkerFunction(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("[WF] Started working...");
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(400);
                Console.WriteLine("[WF] Iteration: " + i);
            }
        }
        
        // la función que indica que terminó ...
        static void FinishFunction(object sender, RunWorkerCompletedEventArgs e)
        {
            Console.WriteLine("[FF] Finished working!");
            doneEvent.Set(); // Signal that the worker has finished
        }

        static int Main(string[] args)
        {
            Console.WriteLine("[Main] Starting Test!");
            // crea un worker
            BackgroundWorker bw = new BackgroundWorker();
            // le asigna la funcio que trabaja
            bw.DoWork += WorkerFunction;
            // le asigna la función que se ejecuta al terminar.
            bw.RunWorkerCompleted += FinishFunction;
            // y lo hacemos correr
            bw.RunWorkerAsync();
            // el worker está trabajando, y el hilo principal se va a seguir ejecutando;
            for (var i=0; i<10 ;i++)
            {
                Thread.Sleep(100);
                Console.WriteLine("[Main] Running code in Main: " + i);
            }
            Console.WriteLine("[Main] Waiting for worker to finish...");
            // esperamos que se ejecute el FinishFunction
            // while (bw.IsBusy) // preguntamos si terminó
            // {
            //     Thread.Sleep(100); // Small delay to avoid excessive CPU usage
            // }  // Simpler, but less efficient
            doneEvent.WaitOne(); // Block until worker signals completion
            // ocurrió el evento de terminación del worker y podemos continuar...
            Console.WriteLine("[Main] Worker finished. Finishing execution...");
            return 0;
        }
    }
}