using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncDesayuno
{
    // Estas clases están intencionadamente vacías para el propósito de
    // este ejemplo. Son simplemente clases marcadoras con el propósito
    // de demostración, no contienen propiedades, y no sirven para ningún
    // otro propósito. Solo sirven como tipo de retorno en los métodos.
    // https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/#review-final-code
    internal class Panceta { }
    internal class Cafe { }
    internal class Huevo { }
    internal class Jugo { }
    internal class Tostada { }

    class Program
    {
        static async Task Main(string[] args)
        {
            DateTime comienzo = DateTime.Now;
            // sirve café sincrónicamente. Es rápido. No hay que porqué esperar.
            Cafe taza = ServirCafe();
            Console.WriteLine("El café está listo.");
            
            // Cosas que se hacen en paralelo ...
            var huevoTask = FreirHuevosAsync(2);
            // asumimos que lo podemos hacer en el mismo sartén o que tenemos
            // sartenes diferentes ...
            var pancetaTask = FreirPancetaAsync(3);
            var tostadaTask = HacerTostadaConMantecaYMermeladaAsync(2);
            // guardamos las tareas en una lista
            var desayunoTasks = new List<Task> { huevoTask, pancetaTask, tostadaTask };
            // Mientras haya tareas ejecutando ...
            while (desayunoTasks.Count > 0)
            {
                //  Se espera a que termine cualquiera de las tareas (WhenAny).
                // Se espera su finalización completa (await),
                Task finishedTask = await Task.WhenAny(desayunoTasks);
                // Luego se informa cuál terminó,
                if (finishedTask == huevoTask)
                {
                    Console.WriteLine("\tLos huevos están listos.");
                }
                else if (finishedTask == pancetaTask)
                {
                    Console.WriteLine("\tLa panceta está lista.");
                }
                else if (finishedTask == tostadaTask)
                {
                    Console.WriteLine("\tLas tostadas están listas.");
                }
                // Asegurarse que termine antes de continuar con el siguiente paso
                await finishedTask;
                // eliminar la tarea completada de la lista
                desayunoTasks.Remove(finishedTask);
            }
            // Finalmente, se sirve el jugo (sincrónicamente) y se imprime que está listo.
            Jugo jugoDeNaranja = ServirJugoDeNaranja();
            Console.WriteLine("\t\tEl jugo de naranja está listo.");
            Console.WriteLine("\t\t¡El desayuno está servido!");
            DateTime final = DateTime.Now;
            TimeSpan duracion = final-comienzo;
            Console.WriteLine($"Duración : {duracion}");
        }

        static async Task<Tostada> HacerTostadaConMantecaYMermeladaAsync(int number)
        {
            var tostada = await TostarPanAsync(number);
            AplicarManteca(tostada);
            AplicarMermelada(tostada);

            return tostada;
        }

        private static Jugo ServirJugoDeNaranja()
        {
            Console.WriteLine("\tSirviendo el jugo de naranja.");
            return new Jugo();
        }

        private static void AplicarMermelada(Tostada tostada) =>
            Console.WriteLine("\tPoniendo mermelada a la tostada.");

        private static void AplicarManteca(Tostada tostada) =>
            Console.WriteLine("\tPoniendo manteca a la tostada.");

        private static async Task<Tostada> TostarPanAsync(int rebanadas)
        {
            for (int rebanada = 0; rebanada < rebanadas; rebanada++)
            {
                Console.WriteLine("\tPoniendo una rodaja de pan a la tostadora.");
            }
            Console.WriteLine("\tComenzar el tostado...");
            await Task.Delay(3000);
            Console.WriteLine("\tQuitar la tostada de la tostadora.");

            return new Tostada();
        }

        private static async Task<Panceta> FreirPancetaAsync(int rebanadas)
        {
            Console.WriteLine($"\tPoniendo {rebanadas} tiras de panceta en el sartén.");
            Console.WriteLine("\tCocinando el primer lado de la panceta...");
            await Task.Delay(3000);
            for (int rebanada = 0; rebanada < rebanadas; rebanada++)
            {
                Console.WriteLine("\tDando vuelta la  tira de panceta");
            }
            Console.WriteLine("\tCocinando el segundo lado de la panceta...");
            await Task.Delay(3000);
            Console.WriteLine("\tPoniendo la panceta en un plato.");

            return new Panceta();
        }

        private static async Task<Huevo> FreirHuevosAsync(int cuantos)
        {
            Console.WriteLine("\tCalentando el sartén para los huevos...");
            await Task.Delay(3000);
            Console.WriteLine($"\tRompiendo {cuantos} huevos");
            Console.WriteLine("\tCocinando los huevos...");
            await Task.Delay(3000);
            Console.WriteLine("\tPoniendo los huevos en un plato.");

            return new Huevo();
        }

        private static Cafe ServirCafe()
        {
            Console.WriteLine("Sirviendo el café en las tazas.");
            return new Cafe();
        }
    }
}