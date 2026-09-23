using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLinkedTokenExample;

class AsyncLinkedTokenExample
{
    static async Task Main()
    {
        // cts es el único CancellationTokenSource "de afuera".
        // Se cancela si el usuario escribe "cancelar".
        using var cts = new CancellationTokenSource();

        // linkedCts queda atado al token de cts.
        // Se cancela por dos razones:
        // 1) se canceló cts (lo arrastra el enlace)
        // 2) pasaron 5 segundos (CancelAfter cancela solo a linkedCts)
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(5));

        Console.WriteLine("Descargando informe.zip (timeout 5 s).");
        Console.WriteLine("Escribí cancelar y Enter para cortarla antes.");

        // La descarga mira el token enlazado.
        // La consola, en paralelo, mira si el usuario escribió "cancelar".
        var descarga = DescargarArchivoAsync(linkedCts.Token);
        var lectura = LeerConsolaAsync(cts, descarga);

        // Seguimos cuando termina cualquiera de las dos.
        await Task.WhenAny(descarga, lectura);

        try
        {
            await descarga;
            Console.WriteLine("Descarga completa.");
        }
        catch (OperationCanceledException)
        {
            // Si cts está cancelado, fue el usuario.
            // Si no, fue el timeout de linkedCts.
            if (cts.IsCancellationRequested)
                Console.WriteLine("Cancelado porque escribiste cancelar.");
            else
                Console.WriteLine("Cancelado por timeout de la descarga.");

            Console.WriteLine($"cts cancelado: {cts.IsCancellationRequested}");
            Console.WriteLine($"linkedCts cancelado: {linkedCts.IsCancellationRequested}");
        }

        // Si la descarga terminó sola, ReadLine sigue esperando una línea.
        // En ese caso pedimos Enter. Si el usuario ya escribió "cancelar", leemos una tecla.
        if (!lectura.IsCompleted)
        {
            Console.WriteLine("Presioná Enter para salir.");
            await lectura;
        }
        else
        {
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }

    // Lee líneas de la consola mientras la descarga sigue.
    // Si la línea es exactamente "cancelar", cancela cts.
    static async Task LeerConsolaAsync(CancellationTokenSource cts, Task descarga)
    {
        while (!descarga.IsCompleted)
        {
            // ReadLine bloquea. Va en otro hilo para no frenar la descarga.
            string? linea = await Task.Run(Console.ReadLine);

            // null: se cerró la entrada. IsCompleted: la descarga ya terminó.
            if (linea == null || descarga.IsCompleted)
                return;

            if (linea == "cancelar")
            {
                Console.WriteLine("Main::Cancel");
                cts.Cancel();
                return;
            }

            Console.WriteLine("Para cancelar escribí: cancelar");
        }
    }

    // Baja el archivo de a un bloque. El token es el de linkedCts,
    // así que corta tanto por "cancelar" como por el timeout de 5 s.
    static async Task DescargarArchivoAsync(CancellationToken token)
    {
        const int bloques = 12;

        for (int i = 1; i <= bloques; i++)
        {
            token.ThrowIfCancellationRequested();

            int bytes = LeerBloque(i, token);
            Console.WriteLine($"Bloque {i}/{bloques} ({bytes} bytes)");

            // Espera entre bloques, como si llegara el próximo por la red.
            await Task.Delay(800, token);
        }
    }

    // Simula el trabajo de leer un bloque: varias sumas, y entre medio
    // pregunta si hay que cancelar.
    static int LeerBloque(int numero, CancellationToken token)
    {
        int bytes = 0;

        for (int paso = 0; paso < 20; paso++)
        {
            token.ThrowIfCancellationRequested();

            for (int n = 0; n < 100000; n++)
                bytes = unchecked(bytes + numero + n);
        }

        return Math.Abs(bytes % 4096) + 1;
    }
}