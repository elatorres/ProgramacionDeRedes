using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

class InfoConsumer
{
    public static async Task Main()
    {
        // La conexión habitual
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        
        // declaramos los nombres del exchange y la cola
        string exchangeName = "logs_topic";
        string queueName = ""; // Sin nombre!!
        
        // El exchange es de tópico
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: false);
        // y la cola es con nombre autogenerado, exclusiva y auto delete.
        // De esa forma se elimina la suscripción al salir.
        queueName = (await channel.QueueDeclareAsync(queueName, durable: false, exclusive: true, autoDelete: true)).QueueName;
        // Ask the user for topics
        Console.WriteLine("[ConsumidorEx] Ingrese los tópicos a los que se suscribe separados por espacios:");
        Console.WriteLine("[ConsumidorEx] Elija entre [info] [warning] [error] sin paréntesis rectos.");
        Console.Write("Tópicos: ");

        string? input = Console.ReadLine();
        input = input?.Trim().ToLowerInvariant();
        
        // Split input into topics (space-separated)
        string[] topics = string.IsNullOrWhiteSpace(input)
            ? new[] { "info" } // default topic
            : input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        // Validate topics and replace invalid ones with "info"
        for (int i = 0; i < topics.Length; i++)
        {
            switch (topics[i])
            {
                case "info":
                case "error":
                case "warning":
                    break;
                default:
                    topics[i] = "info";
                    break;
            }
        }
        
        // Register each topic
        foreach (var topic in topics)
        {
            string routingKey = $"*.{topic}";
            await channel.QueueBindAsync(queueName, exchangeName, routingKey);
            Console.WriteLine($"[ConsumidorEx] Suscripto al tópico: '{routingKey}'");
        }
        
        // Preparo el consumidor
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            // Aquí va a recibir los mensajes
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            Console.WriteLine($"[InfoConsumidorEx] Recibido: '{ea.RoutingKey}':'{message}'");
            await Task.Yield();
        };
        
        // Comenzamos a recibir mensajes
        await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);
        // enter para salir.
        Console.ReadLine();
        
        // Unsubscribe not needed
        Console.WriteLine("¡Chaucito!");
    }
}
