using System;
using System.Threading.Tasks;

namespace AsyncSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Creo la tarea que devuelve un string ¿?
            Task<string> task1 = Task.Run(() =>
            {
                // que devuelve 12 que es un entero
                return 12;
                // Pero en realidad devuelve Task<int> !!
                // con ContinueWith recibimos el resultado de la primera tarea
                // y lo llamamos 'antecedente'
            }).ContinueWith((antecedente) =>
            {
                // y la segunda tarea (que en realidad es la task1), esta sí devuelve el string ...
                return $"El cuadrado de {antecedente.Result} es: {antecedente.Result * antecedente.Result}"; 
            });
            // y a la promesa le pedimos el resultado. Hay un await implícito al pedir el resultado.
            // En programas de consola no es mucho problema. Pero no es conveniente este uso en programas de GUI o web.
            // Console.WriteLine(task1.Result);  // Si Main fuera sincrónico
            string resultado = await task1;
            Console.WriteLine(resultado);
            Console.ReadKey();
        }
    }
}