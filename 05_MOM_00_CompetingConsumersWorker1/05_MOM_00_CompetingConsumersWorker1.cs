using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

internal class Program
{
    static async Task Main()
    {
        // El patrón competing consumers, varias aplicaciones consumidoras escuchan la misma cola.
        // Este patrón se utiliza para distribuir la carga de trabajo del procesamiento de mensajes
        // y lograr el escalado horizontal del lado consumidor de un sistema de mensajería.
        
        // Creo la factory, la conexión y el canal
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        
        // vinculo el canal a la cola persistente
        await channel.QueueDeclareAsync("task_queue", true, false,
            false, null);
        
        // El canal espera a que haya hecho la confirmación del procesamiento
        await channel.BasicQosAsync(0, 1, false);
        
        // Creamos el consumidor
        var consumer = new AsyncEventingBasicConsumer(channel);
        
        // Le asignamos la función delegada (el worker) a nuestro consumidor
        consumer.ReceivedAsync += (o, args) => OnMessageReceivedAsync(o, args);
        
        // Enciendo el consumidor con Ack/confirmación manual ... // En vez de autoack:true
        await channel.BasicConsumeAsync("task_queue", false, consumer);
        
        // Y nos ponemos a tomar mate ...
        Console.WriteLine(" [*] Waiting for messages.");
        
        // Y espero un teclazo para salir
        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    } // Fin del Main
    
    // Esta es la función con nombre. Nótese que recibe el sender y el eventArgs
    static async Task  OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        //decodificamos el mensaje
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        
        // Opcionalmente llamo a otro método para hacer el procesamiento complejo ¿?
        await ProcessMessageAsync(sender, ea, message); //¿Qué pasa si no pongo el await?
        // Ya procesé ...
        
        // Obtengo el canal por el que me enviaron el mensaje
        var channel = ((AsyncEventingBasicConsumer)sender).Channel;
        
        // y le aviso que terminé
        await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
    } // A esperar por otro mensaje
    
    // Ejemplo de procesamiento posterior a la recepción
    static async Task  ProcessMessageAsync(object sender, BasicDeliverEventArgs ea, string message)
    {
        Console.WriteLine($"Processing message: {message}");
        // por cada punto en el mensaje tardo un segundo en procesar el mensaje
        // simulo dificultad en procesarlo
        int dots = message.Split('.').Length - 1;
        // tardo en procesar el mensaje 
        await Task.Delay(dots * 1000);
        // Aviso que terminé
        Console.WriteLine(" [x] Done");
    }
}