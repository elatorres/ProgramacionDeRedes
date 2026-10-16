using RabbitMQ.Client;
using System.Text;
//#=============================================================================#
//#  La solución 05_MOM_02_DeadletterProducer genera mensajes en RabbitMQ       #
//#  que son recibidos por 05_MOM_02_DeadletterConsumer el cual los cancela     #
//#  entonces pasan a la Dead Letter Queue, donde 05_MOM_02_DeadletterConsumer2 #
//#  los contabiliza y muestra un mensaje. 05_MOM_02_DeadletterShared es classe #
//#=============================================================================#


class Producer
{
    static async Task Main()
    {
        using var connection = await Shared.Setup.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        // Méthodo compartido para declarar en MOM
        await Shared.Setup.DeclareQueuesAsync(channel);

        Console.WriteLine("Type your message and press ENTER to send.");
        Console.WriteLine("Press ENTER without typing to send the default message.");
        Console.WriteLine("Type 'exit' to quit.");
        Console.WriteLine("----------------------------\n");

        while (true)
        {
            Console.Write("Message: ");
            string? input = Console.ReadLine();

            // condición de salida
            if (input?.Trim().ToLower() == "exit")
            {
                break;
            }

            // mensaje por defecto
            string message = string.IsNullOrWhiteSpace(input) 
                ? "Hello RabbitMQ with DLQ (Default)!" 
                : input;

            var body = Encoding.UTF8.GetBytes(message);

            // publicar a la cola principal main.queue
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: "main.queue",
                mandatory: true,
                body: body);

            Console.WriteLine($"> Sent: {message}");
        }

        Console.WriteLine("Producer closed.");
    }
}