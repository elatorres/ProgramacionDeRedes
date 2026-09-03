using System;
using System.Threading.Tasks;

namespace AsyncDesayuno
{
    // Estas clases están intencionadamente vacías para el propósito de
    // este ejemplo. Son simplemente clases marcadoras con el propósito
    // de demostración, no contienen propiedades, y no sirven para ningún
    // otro propósito.
    internal class Panceta { }
    internal class Cafe { }
    internal class Huevo { }
    internal class Jugo { }
    internal class Tostada { }

    class Program
    {
        static void Main(string[] args)
        {
            DateTime comienzo = DateTime.Now;
            Cafe cup = ServirCafe();
            Console.WriteLine("El café está listo.");

            Huevo huevos = FreirHuevos(2);
            Console.WriteLine("Los huevos están listos.");

            Panceta panceta = FreirPanceta(3);
            Console.WriteLine("La panceta está lista.");

            Tostada tostada = TostarPan(2);
            AplicarManteca(tostada);
            AplicarMermelada(tostada);
            Console.WriteLine("Las tostadas están listas.");

            Jugo oj = ServirJugoDeNaranja();
            Console.WriteLine("El jugo de naranja está listo.");
            Console.WriteLine("¡El desayuno está servido!");
            DateTime final = DateTime.Now;
            TimeSpan duracion = final-comienzo;
            Console.WriteLine($"Duración : {duracion}");
        }

        private static Jugo ServirJugoDeNaranja()
        {
            Console.WriteLine("Sirviendo el jugo de naranja.");
            return new Jugo();
        }

        private static void AplicarMermelada(Tostada tostada) =>
            Console.WriteLine("Poniendo mermelada a la tostada.");

        private static void AplicarManteca(Tostada tostada) =>
            Console.WriteLine("Poniendo manteca a la tostada.");

        private static Tostada TostarPan(int rebanadas)
        {
            for (int rebanada = 0; rebanada < rebanadas; rebanada++)
            {
                Console.WriteLine("Poniendo una rodaja de pan a la tostadora.");
            }
            Console.WriteLine("Comenzar el tostado...");
            Task.Delay(3000).Wait();
            Console.WriteLine("Quitar la tostada de la tostadora.");

            return new Tostada();
        }

        private static Panceta FreirPanceta(int rebanadas)
        {
            Console.WriteLine($"Poniendo {rebanadas} tiras de panceta en el sartén.");
            Console.WriteLine("Cocinando el primer lado de la panceta...");
            Task.Delay(3000).Wait();
            for (int rebanada = 0; rebanada < rebanadas; rebanada++)
            {
                Console.WriteLine("Dando vuelta la  tira de panceta");
            }
            Console.WriteLine("Cocinando el segundo lado de la panceta...");
            Task.Delay(3000).Wait();
            Console.WriteLine("Poniendo la panceta en un plato.");

            return new Panceta();
        }

        private static Huevo FreirHuevos(int Cuantos)
        {
            Console.WriteLine("Calentando el sartén para los huevos...");
            Task.Delay(3000).Wait();
            Console.WriteLine($"Rompiendo {Cuantos} huevos");
            Console.WriteLine("Cocinando los huevos...");
            Task.Delay(3000).Wait();
            Console.WriteLine("Poniendo los huevos en un plato.");

            return new Huevo();
        }

        private static Cafe ServirCafe()
        {
            Console.WriteLine("Sirviendo el café en las tazas.");
            return new Cafe();
        }
    }
}