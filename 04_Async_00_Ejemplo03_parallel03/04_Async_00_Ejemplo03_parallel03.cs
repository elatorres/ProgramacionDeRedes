using System;
using System.Threading.Tasks;

class Program
{
    // cd ~/RiderProjects/ProgramacionDeRedes/04_Async_00_Ejemplo03_parallel03/bin/Debug/net8.0
    static void Main(string[] args)
    {
        Console.WriteLine("04_Async_00_Ejemplo03_parallel03\n parámetros: {0}", args.Length);
        long totalSize = 0;
        // cuento los parámetros por línea de comando
        if (args.Length == 0)
        {
            Console.WriteLine("No hay parámetros en la línea de comandos.");
            return; // entonces termino ....
        }
        //  Si hay un parámetro asumo que es el camino de un directorio. Veo si existe...
        if (!Directory.Exists(args[0]))
        {
            Console.WriteLine("El directorio no existe.");
            return;
        }
        // obtengo la lista de los archivos que hay allí...
        String[] files = Directory.GetFiles(args[0]);
        // y en paralelo para cada archivo ....
        Parallel.For(0, files.Length, index =>
        {
            // pido la información del archivo ....
            FileInfo fi = new FileInfo(files[index]);
            // veo el tamaño ...
            long size = fi.Length;
            // y lo sumo al total controlando la concurrencia
            // https://learn.microsoft.com/en-us/dotnet/api/system.threading.interlocked?view=net-9.0
            // https://dotnettutorials.net/lesson/interlocked-vs-lock-in-csharp/
            Interlocked.Add(ref totalSize, size);
        });
        // y mostramos el resultado
        Console.WriteLine("Directorio '{0}':", args[0]);
        Console.WriteLine("    {0:N0} archivos, {1:N0} bytes", files.Length, totalSize);
    }
}