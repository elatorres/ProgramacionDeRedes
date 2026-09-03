namespace AsyncSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Task t1 = new Task(PrintInfo); // Creo la tarea
            t1.Start(); // inicio la tarea 
            /* // También se puede crear así:
             Task t1 = Task.Run(() => { PrintInfo(); }); */ 
            Console.WriteLine("Hilo Principal terminado.");
            Console.ReadKey();
        }

        static void PrintInfo()
        {
            for (int i = 1; i <= 4; i++)
            {
                Console.WriteLine("Valor de i: {0}", i);
            }
            Console.WriteLine("Hilo hijo terminado");
        }
    }
}