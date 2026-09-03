using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Collections.Concurrent;

// Este cliente se conecta sincrónicamente a RabbitMQ, Ver. 5.0.0
// envía un mensaje a "rpc_queue", y se bloquea hasta que vuelve una respuesta.
public class RpcClient : IDisposable
{
    private const string QUEUE_NAME = "rpc_queue";

    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string replyQueueName;
    private readonly EventingBasicConsumer consumer;
    private readonly BlockingCollection<string> respQueue = new();
    private string? correlationId;

    public RpcClient()
    {
        // Crear una conexión y un canal
        var factory = new ConnectionFactory() { HostName = "localhost" };
        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        // Declarara uns cola temporal de respuesta
        replyQueueName = channel.QueueDeclare().QueueName;

        // El consumidor que va a recibir la respuesta
        consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            // verificar el correlation ID
            if (ea.BasicProperties.CorrelationId == correlationId)
            {
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);
                respQueue.Add(response);
            }
        };

        // comenzar a consumir de la cola de resultados
        channel.BasicConsume(
            consumer: consumer,
            queue: replyQueueName,
            autoAck: true);
    }

    // Mandar un mensaje
    public string Call(string message)
    {
        correlationId = Guid.NewGuid().ToString();

        var props = channel.CreateBasicProperties();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName; // devolver ene sta cola

        var messageBytes = Encoding.UTF8.GetBytes(message);

        // Publicar la solicitud a la cola de  RPC
        channel.BasicPublish(
            exchange: "",
            routingKey: QUEUE_NAME,
            basicProperties: props,
            body: messageBytes);

        // Bloquearse esperando hasta que la respuesta con el correlationId correcto llegue
        var response = respQueue.Take();
        return response;
    }

    // liberamos los recursos
    public void Dispose()
    {
        channel?.Close();
        connection?.Close();
    }
}

public class Rpc
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
            string mensaje = Console.ReadLine();
            // si es exit salgo.
            if (mensaje == "exit")
                break;
            // si es vacío mando Hello World!
            if (mensaje == "")
                mensaje="Hello World!";
            
            Console.WriteLine($" [>] Sending: {mensaje}");
            var response = rpcClient.Call(mensaje);
            Console.WriteLine($" [<] Received: {response}");
        }
    }
}
