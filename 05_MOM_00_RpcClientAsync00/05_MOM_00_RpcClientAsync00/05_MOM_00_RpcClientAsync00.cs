// File: RpcClientAsync.cs
using RabbitMQ.Client;  // version 5.0.0
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;

// Un cliente sincrónico!!
public class RpcClient : IAsyncDisposable
{
    // ya declaro mi conexión
    private readonly IConnection connection;
    // mi canal
    private readonly IModel channel;
    // mi cola de respuesta
    private readonly string replyQueueName;
    // y mi consumidor ...
    private readonly EventingBasicConsumer consumer;
    // y me hago un diccionario para guardar la lista de los mensajes de respuesta...
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> 
            pendingResponses = new();
    // todos sin inicializar
    // Nos creamos un cliente
    public RpcClient()
    {
        // en la inicialización del RpcClient ...
        // Este tiene una fábrica
        var factory = new ConnectionFactory { HostName = "localhost" };
        // Este tiene una conexión
        connection = factory.CreateConnection();
        // Este tiene un canal.
        channel = connection.CreateModel();
        // hacemos una cola de respuesta
        replyQueueName = channel.QueueDeclare().QueueName;
        // y creamos un consumidor que se maneja por eventos
        consumer = new EventingBasicConsumer(channel);
        // al consumidor le damos una delegada que va a ejecutar cuando tiene un mensaje
        consumer.Received += (model, ea) =>
        {
            // ea es el contenedor con los datos del mensaje recibido.
            // ea.body es el mensaje en binario
            // ea.BasicProperties.CorrelationId es el identificador único del mensaje recibido
            // pendingResponses es un diccionario donde se guardan las solicitudes
            // pendingResponses.TryRemove trata de borrar la solicitud del diccionario y la devuelve en tcs
            // si existe pendiente con el correlation id, obtengo el TaskCompletionSource correspondiente
            if (pendingResponses.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
            {
                // obtenemos el mensaje 
                var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                // Setea el resultado con la cadena de respuesta y completa la tarea
                tcs.TrySetResult(response);
            }
        };
        // le dice al canal que comience a leer mensajes de replyQueueName con auto-acknowledge 
        channel.BasicConsume(consumer: consumer, queue: replyQueueName, autoAck: true);
    }
    
    // El RpcClient tiene un CallAsync (Asincrónico) que recibe un mensaje
    public Task<string> CallAsync(string message)
    {
        // se le asigna un Guid.
        var corrId = Guid.NewGuid().ToString();
        // Creamos propiedades del mensaje
        var props = channel.CreateBasicProperties();
        // le ponemos el ID
        props.CorrelationId = corrId;
        // y le asignamos la cola de respuesta
        props.ReplyTo = replyQueueName;
        // El mensaje lo pasamos a binario ...
        var messageBytes = Encoding.UTF8.GetBytes(message);
        // Creamos el TaskCompletionSource
        var tcs = new TaskCompletionSource<string>();
        // y lo agregamos al diccionario usando el Id como clave.
        pendingResponses[corrId] = tcs;
        // Y mandamos el mensaje por el canal para que se conecte a la cola rpc_queue
        channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: props, body: messageBytes);
        // y devolvemos el TaskCompletionSource tcs
        return tcs.Task;
    }
    
    // hacemos el Dispose asincrónico del objeto
    public async ValueTask DisposeAsync()
    {
        await Task.Run(() =>
        {
            channel.Close();
            connection.Close();
        });
    }
}

// ------------------------------------------
// Main Application
// ------------------------------------------
class Program
{
    static async Task Main(string[] args)
    {
        // creo a mi cliente
        var rpcClient = new RpcClient();
        // hago una solicitud ...
        var request1 = rpcClient.CallAsync("programacion de redes");
        // hago otra solicitud
        var request2 = rpcClient.CallAsync("la mejor materia de todas");
        // hago otras cosas ...
        Console.WriteLine(" [x] Sent async requests. Doing other work...");
        // Simulo que estoy trabajando ...
        await Task.Delay(100);
        Console.WriteLine(" [x] Checking for responses...");
        // verifico si me respondieron la primera
        if (request1.IsCompleted)
            Console.WriteLine(" [.] Response1: " + request1.Result);
        else
            Console.WriteLine(" [.] Response1 still pending");
        // espero a que todas hayan terminado 
        await Task.WhenAll(request1, request2);
        // e imprimo los resultados
        Console.WriteLine(" [✔] Final Response1: " + await request1);
        Console.WriteLine(" [✔] Final Response2: " + await request2);
    }
}
