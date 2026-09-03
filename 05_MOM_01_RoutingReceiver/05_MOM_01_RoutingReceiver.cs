using System.Text;
using RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class Receiver
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Ingrese los tipos de mensaje que quiere recibir:");
        Console.WriteLine(" [info] [warning] [error] sin paréntesis rectos.");
        string message;
        message = Console.ReadLine();
        // Define the delimiter (space character)
        char[] delimiters = new char[] { ' ' };
        // Use the Split method
        string[] words = message.Split(delimiters);

        // Los mecanismos de conexión habituales
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchange: "direct_logs", type: ExchangeType.Direct);
        
        // declare a server-named queue
        var queueDeclareResult = await channel.QueueDeclareAsync();
        string queueName = queueDeclareResult.QueueName;
        
        // para cada una de las severidades/routingkeys 
        foreach (string? severity in words)
        {
            // vincular la cola a esa routingkey
            await channel.QueueBindAsync(queue: queueName, exchange: "direct_logs", routingKey: severity);
            Console.WriteLine("Esperando mensajes de {0}.",severity);
        }

        Console.WriteLine(" [*] Waiting for messages.");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (model, ea) =>
        {
            // solo va a recibir mensajes con las routing keys elegidas por consola.
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;
            Console.WriteLine($" [x] Received '{routingKey}':'{message}'");
            return Task.CompletedTask;
        };
        // comenzar a escuchar mensajes con auto-acknowledge.
        await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
}