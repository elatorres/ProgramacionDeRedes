using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Collections.Concurrent;
// Las nuevas versiones son solo asincrónicas!!
// Este cliente se conecta a RabbitMQ, Ver. 7.2.2
// envía un mensaje a "rpc_queue", y se bloquea hasta que vuelve una respuesta.
public class RpcClient : IDisposable
{
    private const string QUEUE_NAME = "rpc_queue";

    private readonly IConnection connection;
    private readonly IChannel channel;              // antes IModel
    private readonly string replyQueueName;
    private readonly AsyncEventingBasicConsumer consumer; // antes EventingBasicConsumer
    private readonly BlockingCollection<string> respQueue = new();
    private string? correlationId;

    public RpcClient()
    {
        // Crear una conexión y un canal (ahora asincrónico -> bloqueamos)
        var factory = new ConnectionFactory() { HostName = "localhost" };
        connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

        // Declara una cola temporal de respuesta
        replyQueueName = channel.QueueDeclareAsync().GetAwaiter().GetResult().QueueName;

        // El consumidor que va a recibir la respuesta
        consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            // verificar el correlation ID
            if (ea.BasicProperties.CorrelationId == correlationId)
            {
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);
                respQueue.Add(response);
            }
            await Task.CompletedTask;
        };

        // comenzar a consumir de la cola de resultados
        channel.BasicConsumeAsync(
            consumer: consumer,
            queue: replyQueueName,
            autoAck: true).GetAwaiter().GetResult();
    }

    // Mandar un mensaje
    public string Call(string message)
    {
        correlationId = Guid.NewGuid().ToString();

        // Ahora BasicProperties se instancia directamente
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = replyQueueName // devolver en esta cola
        };

        var messageBytes = Encoding.UTF8.GetBytes(message);

        // Publicar la solicitud a la cola de RPC
        channel.BasicPublishAsync(
            exchange: "",
            routingKey: QUEUE_NAME,
            mandatory: false,
            basicProperties: props,
            body: messageBytes).GetAwaiter().GetResult();

        // Bloquearse esperando hasta que la respuesta con el correlationId correcto llegue
        var response = respQueue.Take();
        return response;
    }

    // liberamos los recursos
    public void Dispose()
    {
        channel?.CloseAsync().GetAwaiter().GetResult();
        connection?.CloseAsync().GetAwaiter().GetResult();
    }
}

public static class Rpc
{
    // mi aplicación
    public static void Main(string[] args)
    {
        Console.WriteLine(" [x] Requesting synchronous RPC call...");
        using var rpcClient = new RpcClient();
        Console.WriteLine("Type a Message to send or exit to terminate.");
        //
        while (true)
        {
            // leo un mensaje de la consola
            string? mensaje = Console.ReadLine();
            // si es exit salgo.
            if (mensaje == "exit")
                break;
            // si es vacío mando Hello World!
            if (mensaje == "")
                mensaje = "Hello World!";

            Console.WriteLine($" [>] Sending: {mensaje}");
            var response = rpcClient.Call(mensaje);
            Console.WriteLine($" [<] Received: {response}");
        }
    }
}