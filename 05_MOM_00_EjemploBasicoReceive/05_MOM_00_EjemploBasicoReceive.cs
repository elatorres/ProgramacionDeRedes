using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
// BasicPublish es el mecanismo más simple El productor publica un mensaje
// a una cola y el consumidor recibe el mensaje consumiéndolo de la cola.

// Creo fabrica de conexiones
var factory = new ConnectionFactory { HostName = "localhost" ,
    UserName = "guest",
    Password = "guest" };
// creo conexión
using var connection = await factory.CreateConnectionAsync();
// creo canal dentro de la conexión 
using var channel = await connection.CreateChannelAsync();
// +-------+   +----------+   +-------+
// |factory|-->|connection|-->|channel| 
// +-------+   +----------+   +-------+

// creo la cola si no existe. El que llegue antes la crea
await channel.QueueDeclareAsync(queue: "hello", durable: false, exclusive: false, autoDelete: false,
    arguments: null);// si ya esta creada, usa la que ya se creo

// Creo que consumidor de mensjaes
var consumer = new AsyncEventingBasicConsumer(channel);
// defino la función delegada que va a consumir mensajes
// Una función delegada que se va a disparar con los eventos)
consumer.ReceivedAsync +=  async Task (model, ea) => 
// delegado a ejecutar
{
    // se ejecuta cuando hay un mensaje para recibir
    // el buffer binario para recibir
    //ea events arguments contiene el mensaje recibido
    byte[] body = ea.Body.ToArray();
    // convierto a texto UTF
    var message = Encoding.UTF8.GetString(body);
    // escribo el mensaje
    Console.WriteLine($" [x] Received {message}");
    // tardo un tiempo en procesar el mensaje (simulo trabajo)
    await Task.Delay(1000);
    //Thread.Sleep(2000);
    //return  Task.CompletedTask;
};

// Prendo la consumición de mensajes, desde la cola hello,
// al consumidor consumer, con autoacknowledge. 
await channel.BasicConsumeAsync("hello", autoAck: true, consumer: consumer);
// Aviso que espero por los mensajes
Console.WriteLine(" [*] Waiting for messages.");
// y espero a que me avisen que se terminó...
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine(); // Acá queda esperando, pero la recepción se hace con la delegada.
