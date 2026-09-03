using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
// El patrón competing consumers, varias aplicaciones consumidoras escuchan la misma cola.
// Este patrón se utiliza para distribuir la carga de trabajo del procesamiento de mensajes
// y lograr el escalado horizontal del lado consumidor de un sistema de mensajería.

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();
// +-------+   +----------+   +-------+
// |factory|-->|connection|-->|channel| 
// +-------+   +----------+   +-------+
// Si no existe la cola "task_queue", la creamos
await channel.QueueDeclareAsync(queue: "task_queue", durable: true, exclusive: false,
    autoDelete: false, arguments: null);
// BasicQosAsync : solo enviame un mensaje, hasta que haga el ACK,
// prefetchCount: 1, Esto es que el canal no  mande un mensaje a un worker
// hasta que haya procesado y haya hecho el acknowledge del mensaje anterior.
await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);  
// Avisamos que estamos "listos" para recibir mensajes
Console.WriteLine(" [*] Waiting for messages.");
// Creamos un consumidor
var consumer = new AsyncEventingBasicConsumer(channel);
// Y le asignamos el método delegado a ejecutar cuando se recibe un mensaje
consumer.ReceivedAsync += async (model, ea) =>
{
    //ea events arguments contiene el mensaje recibido
    byte[] body = ea.Body.ToArray();
    // decodificamos el array de bytes a texto
    string message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] Received {message}");
    
    // por cada punto en el mensaje tardo un segundo en procesar el mensaje
    // simulo dificultad en procesarlo
    int dots = message.Split('.').Length - 1;
    // tardo en procesar el mensaje 
    await Task.Delay(dots * 1000);
    // Aviso que terminé
    Console.WriteLine(" [x] Done");

    // aquí también el canal puede ser accedido como ((AsyncEventingBasicConsumer)sender).Channel
    // var channel = ((AsyncEventingBasicConsumer)sender).Channel;
    // no uso el channel del alcance exterior sino que lo extraigo del
    // le aviso a RabbitMQ que terminé de procesar el mensaje (así me puede enviar otro)
    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
    //Terminé de procesar el mensaje
};

// Enciendo el consumidor... Ack manual ... // En vez de autoack:true
await channel.BasicConsumeAsync("task_queue", autoAck: false, consumer: consumer);
// Espero un teclazo para salir
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();