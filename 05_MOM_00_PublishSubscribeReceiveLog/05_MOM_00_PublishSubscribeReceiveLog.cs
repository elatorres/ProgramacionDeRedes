using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

// Este es el suscriptor que se suscribe a un exchange de tipo fanout
// Él y todos los otros suscriptores van a recibir los mismos mensajes

// Los mensajes se mandan a un exchange que enruta a diferentes colas
// según diferentes estrategias. direct, topic, headers y fanout
// Los receptores se suscriben a las colas. Es un método de hacer fanout/difusión 
var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// creo un exchange con nombre (antes usabamos el  exchange "") del tipo Fanout
await channel.ExchangeDeclareAsync(exchange: "logs",
    type: ExchangeType.Fanout);

// declaro una cola server-named queue "amq.gen-ABC123..."
QueueDeclareOk queueDeclareResult = await channel.QueueDeclareAsync();
string queueName = queueDeclareResult.QueueName; // nombre aleatorio pero mío "amq.gen-ABC123..."

// y la conectamos al exchange de arriba
await channel.QueueBindAsync(queue: queueName, exchange: "logs", routingKey: string.Empty);

Console.WriteLine(" [*] Waiting for logs.");

// configuramos nuestro consumidor
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] {message}");
    return Task.CompletedTask;
};

// activamos la escucha
await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

// y esperamos
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();