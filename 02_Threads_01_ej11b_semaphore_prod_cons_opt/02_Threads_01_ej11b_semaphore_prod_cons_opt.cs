﻿using System;
using System.Threading;
using System.Diagnostics;

namespace ProductoresConsumidores
{
// mis constantes
    public static class MyConsts
    {
        public const int CAP_MAX_BUFFER_ITEM = 20; // cantidad de lugares del array de items.
        public const int CANT_MAX_HILOS_PROD = 10; // cantidad de hilos productores
        public const int CANT_MAX_HILOS_CONS = 10; // cantidad de hilos consumidores
        public const int CANT_MAX_ITEMS_PROD = 10; // cantidad de items producidos
        public const int CANT_MAX_ITEMS_CONS = 10; // cantidad de items consumidos
        // Conviene que:
        // (CANT_MAX_HILOS_CONS*CANT_MAX_ITEMS_CONS) = (CANT_MAX_HILOS_PROD*CANT_MAX_ITEMS_PROD)
        // de lo contrario puede quedar productos sin consumir o consumidores sin producto
        // y el programa no termina ...
        // Probar con más productores o más consumidores
        public const int
            TIEMPO_INSERTAR_EXTRAER = 200; // cantidad de milisegundos necesarios para insertar o extraer un item 
    }

    internal class BufferCircularInt
    {
        // BOLETERA
        // capacidad de la boletera, debe coincidir con CAP_MAX_BUFFER_ITEM
        private int[] buffer = new int[MyConsts.CAP_MAX_BUFFER_ITEM];
        private int cantidadLleno = 0; // Cantidad de lugares ocupados
        private int head = 0; // Apunta al siguiente elemento a consumir
        private int tail = 0; // Apunta al siguiente elemento a producir
        // Los semáforos
        private Semaphore consumerLock = new Semaphore(0, MyConsts.CAP_MAX_BUFFER_ITEM);
        private Semaphore producerLock = new Semaphore(MyConsts.CAP_MAX_BUFFER_ITEM, MyConsts.CAP_MAX_BUFFER_ITEM);
        private Semaphore MutalExclusion = new Semaphore(1, 1);

        // Método Productor le pasamos el id para saber qué hilo está insertando
        public void InsertarEnBuffer(int item)
        {
            // Con exclusión mutua !!!
            // bloquea si está lleno
            producerLock.WaitOne();
            // bloqueo para modificar el buffer y punteros asociados
            MutalExclusion.WaitOne();
            buffer[tail] = item;
            tail = (tail + 1) % MyConsts.CAP_MAX_BUFFER_ITEM;
            cantidadLleno++;
            // terminé de cambiar datos
            MutalExclusion.Release();
            // y desbloqueamos un consumidor
            consumerLock.Release();
        }

        // Método Consumidor le pasamos el id para saber qué hilo está extrayendo
        public int ExtraerDeBuffer()
        {
            // esperar si está vacío ...
            consumerLock.WaitOne();
            // bloqueo para modificar el buffer y punteros asociados
            MutalExclusion.WaitOne();
            var item = buffer[head];
            head = (head + 1) % MyConsts.CAP_MAX_BUFFER_ITEM;
            cantidadLleno--;
            // terminamos de cambiar los punteros
            MutalExclusion.Release();
            // Despertar un hilo productor...
            producerLock.Release();
            // devolver el ítem extraído
            return item;
        }
    } // Fin de BOLETERA

    // ========================================================
    // Bufer de items a intercambiar
    internal class BufferCircularItem
    {
        private int[] buffer = new int[MyConsts.CAP_MAX_BUFFER_ITEM];
        private int cantidadLleno = 0;

        // Método Productor le pasamos el id para saber qué hilo está insertando
        public void ProducirBuffer(int id, int item, int pos)
        {
            // Sin exclusión mutua !!!
            buffer[pos] = item; // simplemente lo pongo en el lugar que dice el boleto
            // Ajusto cantidad de lugares ocupados
            lock (this) cantidadLleno++;
            Thread.Sleep(MyConsts.TIEMPO_INSERTAR_EXTRAER);
            Console.WriteLine(
                $"\t\t[Productor {id}] Produjo: {item} | Buffer ocupado: {cantidadLleno}/{MyConsts.CAP_MAX_BUFFER_ITEM}.");
        }

        // Método Consumidor le pasamos el id para saber qué hilo está extrayendo
        public int ConsumirBuffer(int id, int pos)
        {
            // Sin exclusión mutua !!!
            var item = buffer[pos]; // Simplemente lo saco del lugar indicado por el boleto
            // Ajusto cantidad de lugares ocupados
            lock (this) cantidadLleno--;
            Thread.Sleep(MyConsts.TIEMPO_INSERTAR_EXTRAER);
            Console.WriteLine(
                $"\t\t[Consumidor {id}] Consume: {item} | Buffer ocupado: {cantidadLleno}/{MyConsts.CAP_MAX_BUFFER_ITEM}.");
            // devolver el ítem extraído
            return item;
        }
    }

// Example usage
    internal class Program
    {
        // Creo mi buffer circular de Ítems
        private static BufferCircularItem buffer = new();

        //Creo las boleteras
        private static BufferCircularInt boleteraProductores = new();
        private static BufferCircularInt boleteraConsumidores = new();

        // Mis productores producen CANT_MAX_ITEMS_PROD números
        private static void Producer(object obj)
        {
            var id = (int)obj; // obtener el ID del hilo
            var rand = new Random(id); // para generar números
            Console.WriteLine($"[Productor {id}] iniciando.");
            // Cada Productor produce CANT_MAX_ITEMS_PROD items
            for (var i = 0; i < MyConsts.CANT_MAX_ITEMS_PROD; i++)
            {
                var item = rand.Next(1, 100);
                var pos = boleteraProductores.ExtraerDeBuffer(); // extraigo el boleto del lugar donde guardar el ítem.
                buffer.ProducirBuffer(id, item, pos); // Lo guardo efectivamente en ese lugar
                Console.WriteLine($"\t[Productor {id}] produjo: {item} en posición {pos}.");
                boleteraConsumidores.InsertarEnBuffer(pos); // devolver el boleto para consumidores
            }

            Console.WriteLine($"[Productor {id}] terminado.");
        }

        // Mis consumidores consumen CANT_MAX_ITEMS_CONS números
        private static void Consumer(object obj)
        {
            var id = (int)obj; // Get thread ID
            Console.WriteLine($"[Consumidor {id}] iniciando.");
            // Cada Consumidor consume CANT_MAX_ITEMS_CONS
            for (var i = 0; i < MyConsts.CANT_MAX_ITEMS_CONS; i++)
            {
                var pos = boleteraConsumidores
                    .ExtraerDeBuffer(); // extraigo el boleto del lugar de donde debo sacar el ítem.
                var item = buffer.ConsumirBuffer(id, pos);
                Thread.Sleep(MyConsts.TIEMPO_INSERTAR_EXTRAER);
                Console.WriteLine($"\t[Consumidor {id}] consumió: {item}.");
                boleteraProductores.InsertarEnBuffer(pos); // devolver el boleto para productores
            }
            Console.WriteLine($"[Consumidor {id}] terminado.");
        }

        private static void Main()
        {
            // mido el tiempo necesario
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            //inicializo boletera de productores
            for (var i = 0; i < MyConsts.CAP_MAX_BUFFER_ITEM; i++)
                boleteraProductores.InsertarEnBuffer(i);
            //boletera de consumidores está vacía

            // Tengo los arrays de productores y consumidores
            Thread[] producerThreads = new Thread[MyConsts.CANT_MAX_HILOS_PROD];
            Thread[] consumerThreads = new Thread[MyConsts.CANT_MAX_HILOS_CONS];

            // Iniciar 10 hilos productores
            for (var i = 0; i < MyConsts.CANT_MAX_HILOS_PROD; i++)
            {
                producerThreads[i] = new Thread(Producer);
                producerThreads[i].Start(i + 1);
            }

            // Iniciar 10 hilos consumidores
            for (var i = 0; i < MyConsts.CANT_MAX_HILOS_CONS; i++)
            {
                consumerThreads[i] = new Thread(Consumer);
                consumerThreads[i].Start(i + 1);
            }

            // Esperar a que todos los hilos terminen
            foreach (var t in producerThreads) t.Join();
            foreach (var t in consumerThreads) t.Join();

            // Indico la duración del proceso
            stopwatch.Stop();
            Console.WriteLine($"Procesamiento completo. Tiempo de ejecución: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}