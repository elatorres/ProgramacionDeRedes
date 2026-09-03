using RabbitMQ.Client;
using System.Text;
// El patrón competing consumers, varias aplicaciones consumidoras escuchan la misma cola.
// Este patrón se utiliza para distribuir la carga de trabajo del procesamiento de mensajes
// y lograr el escalado horizontal del lado consumidor de un sistema de mensajería.

// factory de conexiones con endpoint
var factory = new ConnectionFactory { HostName = "localhost" ,
    UserName = "guest",
    Password = "guest" };
// creo conexión
using var connection = await factory.CreateConnectionAsync();
// creo canal en conexión
using var channel = await connection.CreateChannelAsync();
// +-------+   +----------+   +-------+
// |factory|-->|connection|-->|channel| 
// +-------+   +----------+   +-------+

// tenemos que ponerle un nuevo nombre porque la cola hello todavía existe.
// creo cola si no existe con nombre task_queue, persistente (!!), compartida, no se autoborra
await channel.QueueDeclareAsync(queue: "task_queue", durable: true, exclusive: false,
    autoDelete: false, arguments: null);

Console.WriteLine($" [x] Escriba el mensaje y Enter. Cada punto es un segundo de retraso en el worker.");
while (true)
{
    //
    var message = GetMessage(); // Declarado más adelante. Lee un mensaje desde la consola. Si es vacío pone "Hello World"
    // Si es "exit" terminamos
    if (message == "exit")
        break;
    // Convertimos a array de bytes
    var body = Encoding.UTF8.GetBytes(message);
    // Precisamos marcar nuestros mensajes como persistentes (best effort)
    var properties = new BasicProperties
    {
        Persistent = true
    };
    // mando mensaje por el canal a la cola designada
    await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "task_queue", 
        mandatory: true, basicProperties: properties, body: body);
    // El mensaje fue enviado a la "task_queue"
    Console.WriteLine($" [x] Sent {message}");
}

static string GetMessage()
{
    // leo de consola 
    string message = Console.ReadLine();
    // si el mensaje es vacío pongo hello world.
    return ((message.Length > 0) ? string.Join(" ", message) : "Hello World!...");
}