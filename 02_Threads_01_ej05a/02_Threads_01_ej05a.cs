using System;
using System.Globalization;
using System.Threading;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("[Main] Starts.");
        Thread t1 = new Thread(() => sum(4, 8));
        t1.Start();
        Console.WriteLine("[Main] Press any key to continue.");
        Console.ReadKey();
        Console.WriteLine("[Main] Ends.");
        return 0;
    }

    static int sum(int a, int b)
    {
        int c = a + b;
        Console.WriteLine("[Sum] La suma de {0} + {1} es: {2}.",a,b,c);
        return c;
    }
}