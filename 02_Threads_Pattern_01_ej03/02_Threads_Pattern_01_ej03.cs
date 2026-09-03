﻿ using System;
 using System.Collections.Generic;
 using System.IO;
 using System.Linq;
 using System.Net.Http;
 using System.Threading;
 using System.Threading.Tasks;
 using System.ComponentModel;
 // Ejemplo 15
 // https://www.codeproject.com/Articles/5352757/Tasks-BackgroundWorkers-and-Threads-Simple-Compari
 // https://medium.com/@lsigenova98/simplifying-asynchrony-in-c-backgroundworker-vs-async-await-e44ccdc48dd9
 namespace TaskPattern
 {
     class Program
     {
         static int Main(string[] args)
         {
             //Comienza en Main ...
             Console.WriteLine("[Main] Starting Test!");
             // Creo un background
             BackgroundWorker bw = new BackgroundWorker();
             // Defino qué función se va a ejecutar en paralelo
             bw.DoWork += WorkerFunction;
             
             // La función que se va a ejecutar cuando termine  DoWork
             bw.RunWorkerCompleted += FinishFunction;
             
             //Mando ejecutar el BachgroundWorker, no se bloquea el hilo principal
             bw.RunWorkerAsync();
             
             // El hilo principal se va a seguir ejecutando;
             for (var i=0; i<10 ;i++)
             {
                 Thread.Sleep(1000);
                 Console.WriteLine("[Main] Running code in Main: " + i);
             }
             Console.WriteLine("[Main] End...");
             // Cuando termine el hilo principal espero a que presionen una tecla...
             Console.WriteLine("[Main] Press a key to continue...");
             Console. ReadKey();
             return 0;
         } // Main
         // Declaro las funciones de los hilos a ejecutar.
         // La función que va a simular trabajo para ejecutarse en paralelo
         static void WorkerFunction(object sender, DoWorkEventArgs e)
         {
             Console.WriteLine("[WF] Started working...");
             for (var i = 0; i < 10; i++)
             {
                 Thread.Sleep(400);
                 Console.WriteLine("[WF] Iteration: "+i);
             }
         }
         
         static void FinishFunction(object sender, RunWorkerCompletedEventArgs e)
         {
             Console.WriteLine("[FF] Finished working!");
         }

         
     } // class Program
 }