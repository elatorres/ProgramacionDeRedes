﻿
using System;
using System.ComponentModel;
using System.Threading;

namespace TaskPattern
{
    class Program
    {
        // ManualResetEvents to signal when workers are done
        private static ManualResetEvent worker1Done = new ManualResetEvent(false);
        private static ManualResetEvent worker2Done = new ManualResetEvent(false);

        // Worker 1 function
        static void WorkerFunction1(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("[WF1] Worker 1 started...");
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(300); // Simulates work
                Console.WriteLine("[WF1] Iteration: " + i);
            }
        }

        // Worker 2 function
        static void WorkerFunction2(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("[WF2] Worker 2 started...");
            for (var i = 0; i < 5; i++) // Less iterations than Worker1
            {
                Thread.Sleep(500); // Simulates a different workload
                Console.WriteLine("[WF2] Iteration: " + i);
            }
        }

        // Function to signal worker 1 is done
        static void FinishFunction1(object sender, RunWorkerCompletedEventArgs e)
        {
            Console.WriteLine("[FF1] Worker 1 finished!");
            worker1Done.Set(); // Signal that worker 1 is done
        }

        // Function to signal worker 2 is done
        static void FinishFunction2(object sender, RunWorkerCompletedEventArgs e)
        {
            Console.WriteLine("[FF2] Worker 2 finished!");
            worker2Done.Set(); // Signal that worker 2 is done
        }

        static int Main(string[] args)
        {
            Console.WriteLine("[Main] Starting Test!");

            // Create BackgroundWorker for worker 1
            BackgroundWorker bw1 = new BackgroundWorker();
            bw1.DoWork += WorkerFunction1;
            bw1.RunWorkerCompleted += FinishFunction1;

            // Create BackgroundWorker for worker 2
            BackgroundWorker bw2 = new BackgroundWorker();
            bw2.DoWork += WorkerFunction2;
            bw2.RunWorkerCompleted += FinishFunction2;

            // Start both workers
            bw1.RunWorkerAsync();
            bw2.RunWorkerAsync();

            Console.WriteLine("[Main] Waiting for both workers to complete...");

            // Wait until both workers signal they are done
            WaitHandle.WaitAll(new WaitHandle[] { worker1Done, worker2Done });

            Console.WriteLine("[Main] Both workers completed. Now exiting.");
            return 0;
        }
    }
}