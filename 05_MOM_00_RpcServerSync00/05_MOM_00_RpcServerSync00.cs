using RabbitMQ.Client; // versión 7.2.2
using RabbitMQ.Client.Events;
using System.Text;

// ¡¡Solución asincrónica!! La librería 7.x ya no tiene API sincrónica

// Declaro la cola
const string QUEUE_NAME = "rpc_queue";
// creo la fábrica de conexiones que conecta a localhost, guest, guest
var factory = new ConnectionFactory { HostName = "localhost" };
// Creo una conexión (ahora CreateConnectionAsync)
using var connection = await factory.CreateConnectionAsync();
// Creo un canal de la conexión ... (ahora CreateChannelAsync)
using var channel = await connection.CreateChannelAsync();

// creo la cola a usar (ahora QueueDeclareAsync con opciones con nombre)
await channel.QueueDeclareAsync(
    queue: QUEUE_NAME,
    durable: true, // <<==
    exclusive: false,
    autoDelete: false,
    arguments: null);
// declaro las reglas de la cola (ahora BasicQosAsync)
await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

// creo un consumidor manejado por eventos conectado al canal (ahora AsyncEventingBasicConsumer)
var consumer = new AsyncEventingBasicConsumer(channel);
// y le digo que comience a atender a la cola ... (ahora BasicConsumeAsync)
await channel.BasicConsumeAsync(queue: QUEUE_NAME, autoAck: false, consumer: consumer);

// al consumidor le asigno la función delegada que va a atender las solicitudes...
// Es asincrónica, pero usamos Task.Delay para simular el trabajo sin bloquear un hilo
consumer.ReceivedAsync += async (model, ea) =>
{
    // el mensaje recibido 
    var body = ea.Body.ToArray();
    // le extraemos las propiedades
    var props = ea.BasicProperties;
    // creamos propiedades del mensaje de retorno (ahora new BasicProperties)
    var replyProps = new BasicProperties
    {
        // y le ponemos el correlationId del recibido al que vamos a enviar ...
        CorrelationId = props.CorrelationId
    };

    // Deserializamos el mensaje...
    string message = Encoding.UTF8.GetString(body);
    // lo mostramos en la consola 
    Console.WriteLine($" [.] Received: {message}");
    // simulamos trabajo (antes Thread.Sleep(500) -> ahora Task.Delay, no bloquea el hilo)
    await Task.Delay(500);

    // Hacemos el nuevo mensaje agregándole Processed: y lo convertimos a mayúsculas ...
    string response = $"Processed: {message.ToUpper()}";
    // lo pasamos a binario
    var responseBytes = Encoding.UTF8.GetBytes(response);

    // y le decimos al canal que lo mande de vuelta ... (ahora BasicPublishAsync con mandatory)
    await channel.BasicPublishAsync(
        exchange: "",
        routingKey: props.ReplyTo!,
        mandatory: false,
        basicProperties: replyProps,
        body: responseBytes);
    // y le damos un ACK al mensaje recibido... (ahora BasicAckAsync)
    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
};

// Esperando solicitudes
Console.WriteLine(" [x] Awaiting RPC requests");
// presione enter para salir...
Console.WriteLine(" [x] Press [ Enter ] to exit");
Console.ReadLine();