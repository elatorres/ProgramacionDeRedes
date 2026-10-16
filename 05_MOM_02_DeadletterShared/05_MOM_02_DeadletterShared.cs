namespace Shared;

using RabbitMQ.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

//#=============================================================================#
//#  La solución 05_MOM_02_DeadletterProducer genera mensajes en RabbitMQ       #
//#  que son recibidos por 05_MOM_02_DeadletterConsumer el cual los cancela     #
//#  entonces pasan a la Dead Letter Queue, donde 05_MOM_02_DeadletterConsumer2 #
//#  los contabiliza y muestra un mensaje. 05_MOM_02_DeadletterShared es classe #
//#=============================================================================#
public class Setup
{
    public static async Task<IConnection> CreateConnectionAsync()
    {
        // Creo un connection factory configurado para mi servidor
        var factory = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        // v7.0+ prefiere conexiones asincrónicas
        return await factory.CreateConnectionAsync();
    }

    public static async Task DeclareQueuesAsync(IChannel channel)
    {
        // el primero que se conecta declara las colas para la interacción
        
        // Dead-letter exchange
        await channel.ExchangeDeclareAsync(
            exchange: "dlx.exchange",
            type: ExchangeType.Direct,
            durable: true);

        // Dead-letter queue
        await channel.QueueDeclareAsync(
            queue: "deadletter.queue",
            durable: true,
            exclusive: false,
            autoDelete: false);
        
        // Conectamos el exchange a la cola con el routing key "dead" 
        await channel.QueueBindAsync(
            queue: "deadletter.queue",
            exchange: "dlx.exchange",
            routingKey: "dead");
        
        // para decirle a la cola principal que en caso de fallar
        // Mandar el mensaje al exchange de la dead queue
        // preparamos los parámetros
        var args = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", "dlx.exchange" },
            { "x-dead-letter-routing-key", "dead" }
        };

        // Main queue with DLQ arguments
        await channel.QueueDeclareAsync(
            queue: "main.queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args); // parámetros  
    }
}