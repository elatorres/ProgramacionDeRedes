using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
//#=============================================================================#
//#  La solución 05_MOM_02_DeadletterProducer genera mensajes en RabbitMQ       #
//#  que son recibidos por 05_MOM_02_DeadletterConsumer el cual los cancela     #
//#  entonces pasan a la Dead Letter Queue, donde 05_MOM_02_DeadletterConsumer2 #
//#  los contabiliza y muestra un mensaje. 05_MOM_02_DeadletterShared es classe #
//#=============================================================================#

class DeadLetterConsumer
{
    static async Task Main()
    {
        using var connection = await Shared.Setup.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (s, ea) =>
        {
            var msg = Encoding.UTF8.GetString(ea.Body.Span);
            Console.WriteLine($"[WARNING DEADLETTER QUEUE MESSAGE] {msg}");
            
            await channel.BasicAckAsync(ea.DeliveryTag, false); 
        };
        // configuramos al consumidor que escuche de la dead letter queue
        await channel.BasicConsumeAsync(
            queue: "deadletter.queue",
            autoAck: false,
            consumer: consumer);

        Console.WriteLine("Listening to DLQ. \nPress ENTER to exit.\n");
        Console.ReadLine();
    }
}