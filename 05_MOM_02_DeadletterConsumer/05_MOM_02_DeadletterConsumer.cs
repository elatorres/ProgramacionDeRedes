using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
//#=============================================================================#
//#  La solución 05_MOM_02_DeadletterProducer genera mensajes en RabbitMQ       #
//#  que son recibidos por 05_MOM_02_DeadletterConsumer el cual los cancela     #
//#  entonces pasan a la Dead Letter Queue, donde 05_MOM_02_DeadletterConsumer2 #
//#  los contabiliza y muestra un mensaje. 05_MOM_02_DeadletterShared es classe #
//#=============================================================================#

class Consumer
{ 
    static async Task Main()
    {
        using var connection = await Shared.Setup.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        // Declaramos con el método compartido
        await Shared.Setup.DeclareQueuesAsync(channel);
        
        await channel.BasicQosAsync(0, 1, false);
        // nuestro consumidor
        var consumer = new AsyncEventingBasicConsumer(channel);
        // le asignamos la delegada
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            // extraemos el mensaje
            var message = Encoding.UTF8.GetString(ea.Body.Span);
            Console.WriteLine($"Received: {message}");

            Console.WriteLine("Rejecting message...");
            
            // rechazamos el mensaje
            await channel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
        };
        
        // configuramos al consumidor a que escuche de la cola principal
        await channel.BasicConsumeAsync(
            queue: "main.queue",
            autoAck: false,
            consumer: consumer);

        Console.WriteLine("Waiting for messages. \nPress ENTER to exit.\n");
        Console.ReadLine();
    }
}