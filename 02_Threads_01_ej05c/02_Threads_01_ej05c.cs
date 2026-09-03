namespace PR_03_p41_EjemploFor
{
    class Program
    {
        static int Main(string[] args)
        {
            for (int i = 0; i < 100; i++)
            {
                int n = i;
                Thread thread = new Thread(() => imprimir(n));
                thread.Start();
            }
            Console.WriteLine("[Main] Ends. Press any key to exit.");
            Console.ReadKey();
            return 0;
        }
        
        static void imprimir(int a)
        {
            Console.Write("  {0}",a);
        }
    }
}