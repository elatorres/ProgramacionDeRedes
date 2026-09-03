using RabbitMQ.Client;
using System.Text;

// Este es el emisor que manda mensajes al los suscriptores en fanout

// Los mensajes se mandan a un exchange que enruta a diferentes colas
// según diferentes estrategias. direct, topic, headers y fanout
// Los receptores se suscriben a las colas. Es un método de hacer fanout/difusión 
// conecto a rabbitmq 
var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// creo un exchange con nombre (antes usabamos el  exchange "") 
await channel.ExchangeDeclareAsync(exchange: "logs", type: ExchangeType.Fanout);

Console.WriteLine($"Enter a message and press ENTER, exit terminates");
while (true)
{
    // leo el mensaje desde la consola
    var message = Console.ReadLine();
    var body = Encoding.UTF8.GetBytes(message);
    await channel.BasicPublishAsync(exchange: "logs", routingKey: string.Empty, body: body);
    Console.WriteLine($" [x] Sent {message}");
    if (message == "exit") break;
    // Console.WriteLine(" Press [enter] to exit.");
    // Console.ReadLine();
}