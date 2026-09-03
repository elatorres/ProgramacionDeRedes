using RabbitMQ.Client; // usamos el SDK/liberia de RABBITMQ
using System.Text;
// BasicPublish es el mecanismo más simple El productor publica un mensaje
// a una cola y el consumidor recibe el mensaje consumiéndolo de la cola.
public class Sender
{
    public static async Task Main(string[] args)
    {
        // Creamos una factory de conexiones
        var factory = new ConnectionFactory
        {
            // Nos conectamos a rabbit creando una conexión sobre TCP/IP:
            // con EndPoint
            HostName = "localhost" ,
            // y authentication
            // Si no se ponen usuario y password se usa guest por defecto
            UserName = "guest",
            Password = "guest"
        };
        // De la factory obtenemos una conexión,
        // nos abstrae del socket y de otros controles necesarios
        using var connection = await factory.CreateConnectionAsync();
        // Establecemos un canal de comunicación mediante la conexión.
        // Cada hilo debe tener uno diferente. Pueden compartir la misma conexión.
        using var channel = await connection.CreateChannelAsync();
        // +-------+   +----------+   +-------+
        // |factory|-->|connection|-->|channel| 
        // +-------+   +----------+   +-------+
        
        // Creo una cola si no existe (idempotente). 
        // nombre hello, efímera (no persistente), pública (no privada o exclusiva),
        // no se autoborra si el último consumidor se dessuscribe
        // si es exclusiva también se autoborra, pensada para uso exclusivo de una conexión.
        await channel.QueueDeclareAsync(
            queue: "hello", durable: false, exclusive: false, autoDelete: false,
            arguments: null);
        // leo mensajes de la consola y los mando
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
            // AMQP soporta/requiere mensajes en binario
            byte[] body = Encoding.UTF8.GetBytes(mensaje);
            // Envío básico sencillo de mensaje
            await channel.BasicPublishAsync(exchange: "", routingKey: "hello",
                body: body); // routingKey = nombre de la cola
            Console.WriteLine($" [x] El mensaje {mensaje} fue enviado"); // no se si se proceso, sólo que se envio
        }
        Console.WriteLine($"Ejemplo Básico Terminado");
    }
}