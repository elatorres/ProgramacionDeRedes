using System;
using System.Threading;

// ejemplo 5
class Sum
{
    public Sum(int op1, int op2)
    {
        Console.WriteLine("[SUm.Sum] Instantiated with values of {0} and {1}.", op1, op2);
        this.op1 = op1;
        this.op2 = op2;
    }

    int op1;
    int op2;
    int result;
    
    public int Result
    {
        get
        {
            return result;
        }
    }
    
    public void Add()
    { 
        //Simulate work
        Thread.Sleep(5000);
        result = op1 + op2;
    }
}

class ThreadData
{
    static void Main()
    {
        Console.WriteLine("[Main] Instantiating the SUn object and passing it the values to add.");
        Sum sum = new Sum(6, 42);
        Console.WriteLine("[Main] Starting a thread using a Sum delegate.");
        Thread thread = new Thread(new ThreadStart(sum.Add));
        thread.Start();
        //Here we are simulating doing some work before blocking on the Add method's completion.
        Console.WriteLine("[Main] Doing other work.");
        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(200);
            Console.Write(".");
        }
        Console.WriteLine("\n[Main] Waiting for Add to finish.");
        // thread.Join();  //  <== UNCOMMENT THIS!!
        Console.WriteLine("[Main] The Result is {0}.", sum.Result);
        Console.WriteLine("[Main] Press any key to finish.");
        Console.ReadKey();
    }
}