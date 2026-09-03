﻿using System;
using System.Threading;
// Ejemplo 9
class PingPong
{
    public void ping(bool running)
    {
        // Se ejecuta con exclusión mutua
        lock(this)
        {
            // mientras no esté corriendo
            if (!running)
            {
                // la pelotita para ¿?
                Monitor.Pulse(this); // y notificamos al otro hilo para que se libere
                return;  // y terminamos
            }
            // Si está corriendo seguimos jugando
            Console.Write("Ping-"); // Soy ping y voy delante de  pong 
            Monitor.Pulse(this); // Dejamos que pong corra (si estuviera bloqueado), liberándolo.
            Monitor.Wait(this); // Y nos ponemos a esperar a que pong complete y nos libere.
        }  // termina la exclusión mutua del método
    } // fin de ping
    
    public void pong(bool running)
    {
        // Se ejecuta con exclusión mutua
        lock(this)
        {
            // mientras no esté corriendo 
            if (!running)
            {
                // la pelotita se para 
                Monitor.Pulse(this); // notificamos a ping para que se libere (si estuviera bloqueado) y continúe
                return; // Y termina
            }
            // Si está corriendo seguimos jugando
            Console.WriteLine("Pong"); // Soy pong
            Monitor.Pulse(this); // Libero a ping para que corra.
            Monitor.Wait(this); // y me pongo a esperar a que pong me libere cuando termine 
        } // fin de exclusión mutua
    } // fin de pong
} // fin de clase PingPong
 
// Encapsulo mis hilos en la clase MyThread ...
class MyThread
{
    // Declaro thread público y pingpong privado
    public Thread thread;
    private PingPong pingpongobject;
    // Creamos un hilo con un nombre y le pasamos el objeto pingpong.
    public MyThread(string name, PingPong pp)
    {
        // ejecuta el método run()
        thread = new Thread(new ThreadStart(this.run));
        pingpongobject = pp;
        thread.Name=name;
        thread.Start();
    }
    // Begin execution of new thread.
    void run()
    {
        if (thread.Name == "Ping")
        {
            // Si el hilo se llama Ping entonces invoca al
            // método ping del objeto pingpong cinco veces
            for (int i = 0; i < 5; i++) pingpongobject.ping(true);
            // Y termina avisando que terminó para asegurarse liberar al otro hilo si aún está corriendo
            pingpongobject.ping(false);
        }
        else
        {
            // Si el hilo no se llama Ping entonces se llama Pong e invoca
            // al método pong del objeto pingpong cinco veces
            for (int i = 0; i < 5; i++) pingpongobject.pong(true);
            // Y termina avisando
            pingpongobject.pong(false);
        }
    }
}

class BouncingBall
{
    public static int Main()
    {
        // inicializo un objeto ping-pong que tiene dós métodos. Uno ping y otro pong.
        PingPong pp = new PingPong();
        // Comienza el juego ...
        Console.WriteLine("Se tira la pelotita a la mesa...");
        // Inicializo el hilo que ejecutará ping y el hilo que ejecutará el pong
        MyThread mythread1 = new MyThread("Ping", pp);
        MyThread mythread2 = new MyThread("Pong", pp);
        // espero a que terminen
        mythread1.thread.Join();
        mythread2.thread.Join();
        // Y termino
        Console.WriteLine("\nLa pelotita terminó de rebotar...   \nPresione una tecla para terminar...");
        Console.ReadKey();
        return 0;
    }
}