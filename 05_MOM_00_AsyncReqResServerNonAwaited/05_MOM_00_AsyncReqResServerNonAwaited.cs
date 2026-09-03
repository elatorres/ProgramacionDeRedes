using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

// Implementa un RPC Remote Procedure Call con RabbitMQ
const string QUEUE_NAME = "rpc_queue";
// Creo el connection factory y especifico el EndPoint y el usuario (puerto por defecto)
var factory = new ConnectionFactory { HostName = "localhost" ,
    UserName = "guest",
    Password = "guest" };
// Creo la conexión
using var connection = await factory.CreateConnectionAsync();  
// creo el canal de comunicación dentro de la conexión anterior
using var channel = await connection.CreateChannelAsync();  
// Creo una cola QUEUE_NAME = "rpc_queue" si no existe previamente
await channel.QueueDeclareAsync(queue: QUEUE_NAME, durable: false, exclusive: false,
    autoDelete: false, arguments: null);

// Declaro la calidad de servicio de la cola. BasicQosAsync garantiza que sólo se envíe un mensaje
// sin acknowledge a un worker a la vez. Útil para el envío justo en RPC. También permite distribuir
// el trabajo si hay varios servidores. Poner prefetch a 10 hace que como máximo un servidor puede
// procesar hasta 10 mensajes a la vez
await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false);

// Crea el consumidor que escucha en la cola los mensajes que vienen por el canal.
var consumer = new AsyncEventingBasicConsumer(channel);

// Define el ReceivedAsync handler que se ejecuta con cada mensaje recibido en forma asincrónica.
consumer.ReceivedAsync += async (object sender, BasicDeliverEventArgs ea) =>
{
    // We do NOT 'await' the processing here. 
    // We start it as a background task so the consumer can loop back 
    // and grab the next message from the queue immediately.
    _ = Task.Run(async () => 
    {
        // identificar el consumidor a partir del sender
        AsyncEventingBasicConsumer cons = (AsyncEventingBasicConsumer)sender;
        // identificar el channel a partir del consumidor, para saber a dónde responder
        IChannel ch = cons.Channel;
        // inicializamos la respuesta vacía
        string response = string.Empty;
        // extraemos el array de bytes del mensaje recibido (parámetro para Fib)
        byte[] body = ea.Body.ToArray();
        // extraemos de las propiedades el CorrelationId que hay que enviar de vuelta al cliente
        IReadOnlyBasicProperties props = ea.BasicProperties;
        var replyProps = new BasicProperties { CorrelationId = props.CorrelationId };

        try
        {
            // extraemos el número del mensaje recibido del array
            var message = Encoding.UTF8.GetString(body);
            int n = int.Parse(message);
            // y mandamos a hacer el cálculo
            Console.WriteLine($" [.] Processing Fib({n}) on Thread {Environment.CurrentManagedThreadId}");
            // Este es el trabajo pesado
            response = Fib(n).ToString();
            // que tarde un poquito más (mi máquina es muy rápida) quitar si tarda mucho.
            await Task.Delay(500);
            // mostramos el resultado cuando terminó
            Console.WriteLine($" [.] Result Fib({message})={response} on Thread {Environment.CurrentManagedThreadId}");
        }
        catch (Exception e)
        {
            Console.WriteLine($" [.] Error: {e.Message}");
        }
        finally
        {
            // convertimos la respuesta a array de bytes
            var responseBytes = Encoding.UTF8.GetBytes(response);
            
            // Y mandamos la respuesta a través de la cola de respuesta al cliente que la solicitó
            await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
                mandatory: true, basicProperties: replyProps, body: responseBytes);
            
            // Hacemos el Acknowledge de que este mensaje específico fue completado
            // para que RabbitMQ sepa que ya no lo debe reenviar
            await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            // Avisamos que enviamos la respuesta
            Console.WriteLine($" [v] Done Fib for ID: {props.CorrelationId}");
        }
    }); // Fin de l método delegado que procesa los mensajes
    // Return immediately to let RabbitMQ give us the next message
    await Task.CompletedTask;
}; // fin del consumidor de RPC

// Comienza a consumir mensajes 
await channel.BasicConsumeAsync(QUEUE_NAME, false, consumer);

// Y espera por la terminación...
Console.WriteLine(" [x] Awaiting RPC requests");
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
// ¡CUIDADO!
// Calcula el número de Fibbonaci recursivamente
// Asume solo entrada de enteros positivos válidos.
// No esperes que esto funcione para números grandes, PUEDE TARDAR MUCHISIMO.
// Y es probablemente la implementación recursiva más lenta posible, PUEDE TARDAR MUCHISIMO.
// Fn=(phi^n-(-phi)^-n)/sqrt(5) donde phi=~1618034...
// Fib(100)=354.224.848.179.261.915.075
static int Fib(int n)
{
    if (n is 0 or 1)
    {
        return n;
    }

    return Fib(n - 1) + Fib(n - 2);
}
