
// Ejemplo 8
internal class Database
{
    public void SaveData(string text)
    {
        lock (this)
        {
            Console.WriteLine("\nDatabase.SaveData - Iniciado.");
            Console.WriteLine("\nDatabase.SaveData - Ejecutándose.");
            for (var i = 0; i < 100; i++)
            {
                Console.Write(text);
                // if (i == 40) throw new Exception(); // <= UNCOMMENT!!
            }

            Console.WriteLine("\nDatabase.SaveData - Finalizado.");
        } // Fin lock!
    }

    private class ThreadLockApp
    {
        public static readonly Database db = new();

        public static void WorkerThreadMethod1()
        {
            Console.WriteLine("WorkerThreadMethod #1 - Iniciado.");
            Console.WriteLine("WorkerThreadMethod #1 - Invocando Database.SaveData.");
            try
            {
                db.SaveData("-");
            }
            catch (Exception e)
            {
                Console.WriteLine("\n    Ocurrió una excepción: {0}", e.Message);
            }
            Console.WriteLine("WorkerThreadMethod #1 - Retornando desde Output.");
        }

        public static void WorkerThreadMethod2()
        {
            Console.WriteLine("WorkerThreadMethod #2 - Iniciado.");
            Console.WriteLine("WorkerThreadMethod #2 - Invocando Database.SaveData.");
            try
            {
                db.SaveData("0");
            }
            catch (Exception e)
            {
                Console.WriteLine("\n    Ocurrió una excepción: {0}", e.Message);
            }
            Console.WriteLine("WorkerThreadMethod #2 - Retornando desde Output.");
        }

        public static int Main()
        {
            var worker1 = new ThreadStart(WorkerThreadMethod1);
            var worker2 = new ThreadStart(WorkerThreadMethod2);
            Console.WriteLine("Principal - Creando hilos de ejecución secundarios.");
            var t1 = new Thread(worker1);
            var t2 = new Thread(worker2);
            t1.Start();
            t2.Start();
            return 0;
        }
    }
}