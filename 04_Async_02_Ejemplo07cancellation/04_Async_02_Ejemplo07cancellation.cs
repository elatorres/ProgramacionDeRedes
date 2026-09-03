using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncCancellationTokenExample;

class AsyncCancellationTokenExample
{
    static async Task Main()
    {
        // creo un cancellation token source
        var source = new CancellationTokenSource();
        // le pido un token al source - constructor
        var token = source.Token;
        // corremos el hacer algo y le pasamos el cancellationToken obtenido
        var task = Task.Run(() => DoSomething(token), token);
        // esperamos un poquito
        await Task.Delay(1000);
        // cancelamos el token, le mandamos el cancel al hacer algo ...
        Console.WriteLine("Main::Cancel");
        source.Cancel();
        // esperamos otro poquito ....
        await Task.Delay(100);
        // esperamos a que termine el task
        await task;
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }

    static async Task DoSomething(CancellationToken token)
    {
        // mientras no se reciba la cancelación ....
        for (int i = 0; i<100;i++)
        {
            // esperamos un poquito para simular que hacemos algo ...
            await Task.Delay(100);
            // Si estamos cancelando ...
            if (token.IsCancellationRequested)
            {
                // avisamos y terminamos
                Console.WriteLine("DoSomething canceled.");
                // Y sale del método ....
                return;
            }
            // sino seguimos haciendo que hacemos algo ...
            Console.WriteLine("DoSomething running... {0}", i);
        }
    }
}
