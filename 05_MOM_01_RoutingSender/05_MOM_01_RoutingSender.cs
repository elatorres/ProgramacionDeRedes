using System.Text;
using RabbitMQ.Client;

public class Sender
{
    public static async Task Main(string[] args)
    {
        // lo habitual para obtener la conexión
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        // Creamos un exchange llamado direct_logs de tipo Directo
        await channel.ExchangeDeclareAsync("direct_logs", ExchangeType.Direct);
        while (true)
        {
            string severity;
            Console.WriteLine("Ingrese el tipo de mensaje que quiere enviar:");
            Console.WriteLine("Elija uno de [info] [warning] [error] sin paréntesis rectos.");
            Console.WriteLine("Ingrese exit para salir");
            severity = Console.ReadLine();
            if (severity == "exit") break;
            
            // Convert to lowercase for case-insensitive comparison
            severity = severity?.ToLowerInvariant();

            // Use a switch expression to check for allowed values
            switch (severity)
            {
                case "info": break;
                case "error": break;
                case "warning": break;
                // Default case: if none of the above match, return "info"
                default: severity = "info"; break;
            };
            
            Console.WriteLine("Ingrese el mensaje que quiere enviar:");
            var message = Console.ReadLine();
            if (message=="") message = "Hello World!";
            Console.WriteLine("El mensaje a ser enviado es: {0}:{1}", severity, message);
            // enviamos el mensaje codificado a bytes en el body
            var body = Encoding.UTF8.GetBytes(message);
            // la routing key es la severidad
            await channel.BasicPublishAsync("direct_logs", severity, body);
            Console.WriteLine($" [x] Sent '{severity}':'{message}'");
        }
        Console.WriteLine("Saliendo de la aplicación.");
    }
}