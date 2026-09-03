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

// Declaro la calidad de servicio de la cola
// BasicQosAsync garantiza que sólo se envíe un mensaje sin acknowledge a un worker a la vez
// Útil para el envío justo en RPC. También permite distribuir el trabajo si hay varios servidores.
await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

// Crea el consumidor que escucha en la cola los mensajes que vienen por el canal.
var consumer = new AsyncEventingBasicConsumer(channel);

// Define el ReceivedAsync handler que se ejecuta con cada mensaje recibido en forma asincrónica.
consumer.ReceivedAsync += async (object sender, BasicDeliverEventArgs ea) =>
{
    // Recibimos el mensaje
    // Genera un consumidor asociado al sender del mensaje
    AsyncEventingBasicConsumer cons = (AsyncEventingBasicConsumer)sender;
    // y del consumidor obtenemos el canal correspondiente
    IChannel ch = cons.Channel;
    
    // inicializamos la variable para la respuesta
    string response = string.Empty;
    
    // Lee el mensaje binario
    byte[] body = ea.Body.ToArray();
    IReadOnlyBasicProperties props = ea.BasicProperties;
    
    // y extraemos el correlationId para la respuesta
    var replyProps = new BasicProperties
    {
        CorrelationId = props.CorrelationId
    };

    try
    {
        //  y convierte el parámetro a un número n
        var message = Encoding.UTF8.GetString(body);
        int n = int.Parse(message);
        
        // Calcula el número de Fibbonaci recursivamente y lo guardamos
        // como string en la respuesta
        Console.WriteLine($" [.] Fib({message})");
        response = Fib(n).ToString();
        Console.WriteLine($" [.] Fib({message})={response}");
    }
    catch (Exception e)
    {
        Console.WriteLine($" [.] {e.Message}");
        response = string.Empty;
    }
    finally
    {
        // para finalizar convertimos el resultado a bytes
        var responseBytes = Encoding.UTF8.GetBytes(response);
        
        //  Manda la respuesta con el mismo ID de correlación utilizado en la solicitud
        // replyProps.correlationId utilizado por el cliente para hacer coincidir la respuesta.
        await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
            mandatory: true, basicProperties: replyProps, body: responseBytes);
        
        // Hace el acknowledge del mensaje original recibido, a mano, cuando termina de procesar...
        await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
        // Si acaso el servidor falla al calcular, y no llega a enviar el Acknowledge, ...
        // RabbitMQ va a poner el mensaje nuevamente en la cola para ser procesado.
    }
};  // fin del consumidor de RPC

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
