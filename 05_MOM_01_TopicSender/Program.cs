using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

class Publisher
{
    public static async Task Main()
    {
        // la conexión habitual
        var factory = new ConnectionFactory() { HostName = "localhost" };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        
        // declaramos un exchange
        string exchangeName = "logs_topic";
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: false);

        Console.WriteLine("[Publisher] === RabbitMQ Topic Publisher ===");
        Console.WriteLine("[Publisher] Tópicos disponibles: info, warning, error");
        Console.WriteLine("[Publisher] Ingrese uno o más tópicos separados por espacios (e.g., 'info warning')");
        Console.WriteLine("[Publisher] Deje en blanco para tópico por defecto: info");
        Console.WriteLine("[Publisher] Escriba 'exit' para salir.\n");

        while (true)
        {
            // leo los tópicos por los que hay que mandar el próximo mensaje
            Console.Write("Topics> ");
            string? input = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (input == "exit")
                break;

            // Default to info if blank
            string[] topics = string.IsNullOrWhiteSpace(input)
                ? new[] { "info" }
                : input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Validate topics
            for (int i = 0; i < topics.Length; i++)
            {
                switch (topics[i])
                {
                    case "info":
                    case "warning":
                    case "error":
                        break;
                    default:
                        topics[i] = "info"; // si errado o vacío entonces va info
                        break;
                }
            }

            Console.Write("Message> "); 
            string message = Console.ReadLine() ?? string.Empty;
            // si el mensaje es vacío entonces va hola mundo
            if (message == "") message="Hello World!"; 
            var body = Encoding.UTF8.GetBytes(message);

            // Por cada tópico publico un mensaje
            foreach (var topic in topics)
            {
                string routingKey = $"app.{topic}";
                var props = new BasicProperties();
                await channel.BasicPublishAsync(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: props,
                    body: body,
                    cancellationToken: CancellationToken.None
                );

                Console.WriteLine($"[Publisher] Enviado:  '{routingKey}':'{message}'");
            }

            Console.WriteLine();
        }

        Console.WriteLine("[Publisher] Terminado.");
    }
}
