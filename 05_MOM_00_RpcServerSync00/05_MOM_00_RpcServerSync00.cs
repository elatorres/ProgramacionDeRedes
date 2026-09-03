using RabbitMQ.Client; // versión 5.0.0
using RabbitMQ.Client.Events;
using System.Text;

// ¡¡Solución sincrónica!! No usa tasks porque responde inmediatamente (menos tiempo de procesamiento)

// Declaro la cola
const string QUEUE_NAME = "rpc_queue";
// creo la fábrica de conexiones que conecta a localhost, guest, guest
var factory = new ConnectionFactory { HostName = "localhost" };
// Creo una conexión
using var connection = factory.CreateConnection();
// Creo un canal de la conexión ...
using var channel = connection.CreateModel();

// creo la cola a usar
channel.QueueDeclare(QUEUE_NAME, false, false, false, null);
// declaro las reglas de la cola
channel.BasicQos(0, 1, false);

// creo un consumidor manejado por eventos conectador al canal
var consumer = new EventingBasicConsumer(channel);
// y le digo que comience a atender a la cola ...
channel.BasicConsume(queue: QUEUE_NAME, autoAck: false, consumer: consumer);

// al consumidor le asigno la función delegada que va a atender las solicitudes...
// Es sincrónica, por lo tanto es bloqueante!!
consumer.Received += (model, ea) =>
{
    // el mensaje recibido 
    var body = ea.Body.ToArray();
    // le extraemos las propiedades
    var props = ea.BasicProperties;
    // creamos propiedades del mensaje de retorno
    var replyProps = channel.CreateBasicProperties();
    // y le ponemos el correlationId del recibido al que vamos a enviar ...
    replyProps.CorrelationId = props.CorrelationId;
    
    // Deserializamos el mensaje...
    string message = Encoding.UTF8.GetString(body);
    // lo mostramos en la consola 
    Console.WriteLine($" [.] Received: {message}");
    // simulamos trabajo
    Thread.Sleep(500);
    
    // Hacemos el nuevo mensaje agregándole Processed: y lo convertimos a mayúsculas ...
    string response = $"Processed: {message.ToUpper()}";
    // lo pasamos a binario
    var responseBytes = Encoding.UTF8.GetBytes(response);
    
    // y le decimos al canal que lo mande de vuelta ...
    channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
    // y le damos un ACK al mensaje recibido...
    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
};

// Esperando solicitudes
Console.WriteLine(" [x] Awaiting RPC requests");
// presione enter para salir...
Console.WriteLine(" [x] Press [ Enter ] to exit");
Console.ReadLine();