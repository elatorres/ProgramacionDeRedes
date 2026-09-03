﻿using System;
using System.Threading;
using System.Diagnostics;

namespace ProductoresConsumidores;
// Con semáforos
// mis constantes
public static class MyConsts
{
    public const int CAP_MAX_BUFFER_ITEM = 20; // cantidad de lugares del array de items.
    public const int CANT_MAX_HILOS_PROD = 5; // cantidad de hilos productores
    public const int CANT_MAX_HILOS_CONS = 10; // cantidad de hilos consumidores
    public const int CANT_MAX_ITEMS_PROD = 20; // cantidad de items producidos

    public const int CANT_MAX_ITEMS_CONS = 10; // cantidad de items consumidos
    // Conviene que:
    // (CANT_MAX_HILOS_CONS*CANT_MAX_ITEMS_CONS) = (CANT_MAX_HILOS_PROD*CANT_MAX_ITEMS_PROD)
    // de lo contrario puede quedar productos sin consumir o consumidores sin producto
    // y el programa no termina ...
    // Probar con más productores o más consumidores

    public const int
        TIEMPO_INSERTAR_EXTRAER = 200; // cantidad de milisegundos necesarios para insertar o extraer un item 
}

// Buffer circular de capacidad MyConsts.CAP_MAX_BUFFER_ITEM
internal class BufferCircular
{
    private int[] buffer = new int[MyConsts.CAP_MAX_BUFFER_ITEM]; // el buffer
    private int cantidadLleno = 0; // Cantidad de lugares ocupados
    private int head = 0; // Apunta al siguiente elemento a consumir
    private int tail = 0; // Apunta al siguiente elemento a producir
    // Los semáforos
    private Semaphore consumerLock = new Semaphore(0, MyConsts.CAP_MAX_BUFFER_ITEM);
    private Semaphore producerLock = new Semaphore(MyConsts.CAP_MAX_BUFFER_ITEM, MyConsts.CAP_MAX_BUFFER_ITEM);
    private Semaphore MutalExclusion = new Semaphore(1, 1);


    // Método Productor le pasamos el id para saber qué hilo está insertando
    public void ProducirBuffer(int id, int item)
    {
        // bloquea si está lleno
        producerLock.WaitOne();
        // ya no está lleno ...
        MutalExclusion.WaitOne();  // bloqueo para modificar el buffer y punteros asociados
        // agregamos un ítem 
        buffer[tail] = item;
        // y tardo un cierto tiempo en agregarlo ...
        Thread.Sleep(MyConsts.TIEMPO_INSERTAR_EXTRAER);
        // y actualizamos los punteros
        tail = (tail + 1) % MyConsts.CAP_MAX_BUFFER_ITEM;
        // y la cantidad
        cantidadLleno++;
        // terminñe de cambiar datos
        MutalExclusion.Release();
        Console.WriteLine(
            $"\t\t[Productor {id}] Produjo: {item} | Buffer ocupado: {cantidadLleno}/{MyConsts.CAP_MAX_BUFFER_ITEM}.");
        // y desbloqueamos un consumidor
        consumerLock.Release();
    }

    // Método Consumidor le pasamos el id para saber qué hilo está extrayendo
    public int ConsumirBuffer(int id)
    {
        // esperar si está vacío ...
        consumerLock.WaitOne();
        // si llegamos hasta acá es porque no está vacío
        // un lugar donde guardar lo consumido
        int item = 0;
        // bloqueo para modificar el buffer y punteros asociados
        MutalExclusion.WaitOne();
        // obtenemos el ítem del array/buffer.  extraemos un ítem
        item = buffer[head];
        // y tarda un cierto tiempo extraerlo ...
        Thread.Sleep(MyConsts.TIEMPO_INSERTAR_EXTRAER);
        // actualizamos punteros
        head = (head + 1) % MyConsts.CAP_MAX_BUFFER_ITEM;
        // ajustamos la cantidad
        cantidadLleno--;
        // terminamos de cambiar los punteros
        MutalExclusion.Release();
        Console.WriteLine(
            $"\t\t[Consumidor {id}] Consume: {item} | Buffer ocupado: {cantidadLleno}/{MyConsts.CAP_MAX_BUFFER_ITEM}.");
        // Despertar un hilo productor...
        producerLock.Release();
        // devolver el ítem extraído
        return item;
    } // Fin ConsumirBuffer 
} // Fin clase BufferCircular

// Mi programa
internal class Program
{
    // Creo mi buffer circular de números
    private static BufferCircular buffer = new();

    // Mis productores producen CANT_MAX_ITEMS_PROD números
    private static void Producer(object obj)
    {
        var id = (int)obj; // Obtener mi ID
        var rand = new Random(id); // tengo mi propio generador de números aleatorios y lo inicializo
        Console.WriteLine($"[Productor {id}] iniciando.");
        // Cada productor produce CANT_MAX_ITEMS_PROD items
        for (var i = 0; i < MyConsts.CANT_MAX_ITEMS_PROD; i++)
        {
            // me invento un producto del uno al cien
            var item = rand.Next(1, 100);
            // y lo pongo en el buffer
            buffer.ProducirBuffer(id, item);
            Console.WriteLine($"\t[Productor {id}] produjo: {item}.");
        }

        Console.WriteLine($"[Productor {id}] terminado.");
    } // productor termina

    // Mis consumidores consumen CANT_MAX_ITEMS_CONS números
    private static void Consumer(object obj)
    {
        var id = (int)obj; // Obtengo mi ID
        Console.WriteLine($"[Consumidor {id}] iniciando.");
        // Cada consumidor consume CANT_MAX_ITEMS_CONS items
        for (var i = 0; i < MyConsts.CANT_MAX_ITEMS_CONS; i++)
        {
            // consume un número
            var item = buffer.ConsumirBuffer(id);
            // y lo muestra 
            Console.WriteLine($"\t[Consumidor {id}] consumió: {item}.");
        }

        Console.WriteLine($"[Consumidor {id}] terminado.");
    } // fin de consumidor

    private static void Main()
    {
        // mido el tiempo necesario para todo esto
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Tengo los arrays de productores y consumidores
        Thread[] producerThreads = new Thread[MyConsts.CANT_MAX_HILOS_PROD];
        Thread[] consumerThreads = new Thread[MyConsts.CANT_MAX_HILOS_CONS];

        // Iniciar hilos de productores
        for (var i = 0; i < MyConsts.CANT_MAX_HILOS_PROD; i++)
        {
            producerThreads[i] = new Thread(Producer);
            producerThreads[i].Start(i + 1);
        }

        // Iniciar hilos de consumidores
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
    } // Fin Main
} // Fin Program