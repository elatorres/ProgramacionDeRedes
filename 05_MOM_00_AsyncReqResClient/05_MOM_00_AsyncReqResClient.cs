using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;

// RPC
// EJECUTARLO CON PARÁMETROS POR LÍNEA DE COMANDO
// PIDE EL FIBONACCI
// cd /home/enrique/RiderProjects/ProgramacionDeRedes/05_MOM_00_AsyncReqResClient/bin/Debug/net8.0/
// ./05_MOM_00_AsyncReqResClient 10
// 
// El método IAsyncDisposable.DisposeAsync() de la interfaz System.IAsyncDisposable
// se implementa cuando se necesita realizar un Dispose asincrónico. DisposeAsync()
// devuelve un ValueTask que representa la operación de eliminación asíncrona.
// https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync

// La clase RpcClient implementa un canal bidireccional con el servidor donde se le pide una operación
// y se espera el retorno del resultado. Maneja las múltiples solicitudes y es reentrante. Esto es que
// puede tener nuevas solicitudes aunque no haya terminado de procesar la anteriores. Con el mensaje
// se envía un id para identificar la solicitud y también la cola privada de respuesta para el cliente. 
public class RpcClient : IAsyncDisposable
{
    // Este es el nombre de la cola que vamos a usar para enviar mensajes al servidor.
    private const string QUEUE_NAME = "rpc_queue";
    
    // creamos una _connectionFactory
    private readonly IConnectionFactory _connectionFactory;
    
    // _callbackMapper: asigna IDs de correlación a TaskCompletionSource<string> (promesa de resultado),
    // para que el cliente pueda esperar una respuesta para una solicitud específica. Puede haber muchos
    // clientes en forma concurrente utilizando el mismo servidor y el mismo cliente puede hacer varias
    // solicitudes aún cuando no le hayan respondido solicitudes anteriores (programación asincrónica).
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>>
        _callbackMapper = new();

    private IConnection? _connection;
    private IChannel? _channel;
    private string? _replyQueueName;
    
    // Constructor configura la conexión a localhost, y opcionalmente a usuario y password
    public RpcClient() // Al crear le podríamos poner parámetros para crearlo con una conexión específica
    {
        _connectionFactory = new ConnectionFactory { HostName = "localhost" ,
            UserName = "guest",
            Password = "guest" };
    }

    public async Task StartAsync()
    {
        // crea las conexiones asincrónicas y el canal
        _connection = await _connectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        
        // declara una cola temporal de respuesta/retorno/resultado
        // con un nombre autogenerado del estilo "amq.gen-ABC123..."
        QueueDeclareOk queueDeclareResult = await _channel.QueueDeclareAsync();
        // extrae el nombre autogenerado de la cola
        _replyQueueName = queueDeclareResult.QueueName;

        // Configura un consumidor asincrónico que escucha a la cola de respuestas
        var consumer = new AsyncEventingBasicConsumer(_channel);
        // Le asignamos el método delegado que se va a disparar con cada mensaje recibido
        consumer.ReceivedAsync += (model, ea) =>
        {
            // recibo el mensaje
            // extraigo el correlationId de los parámetros del mensaje
            string? correlationId = ea.BasicProperties.CorrelationId;
            // me aseguro que haya un correlationId, sino ignoro el mensaje
            if (false == string.IsNullOrEmpty(correlationId))
            {
                // correlaciona el ID a la tarea y completa su respuesta
                // _callbackMapper es un ConcurrentDictionary<string, TaskCompletionSource<string>>
                // o sea que devuelve Task<string> (tcs promesa de string)
                if (_callbackMapper.TryRemove(correlationId, out var tcs))
                {
                    // Extraigo la respuesta que mandó el servidor, un string que representa un número
                    var body = ea.Body.ToArray();
                    var response = Encoding.UTF8.GetString(body);
                    // Y se la meto al resultado asincrónico/promesa
                    tcs.TrySetResult(response);
                    // Esto desbloquea la tarea asincrónica que quedó esperando por esta respuesta
                }
            }
            return Task.CompletedTask; // Terminé correctamente y asincrónicamente
        }; // Fin del método delegado
        // El consumidor comienza a consumir mensajes de la cola...
        await _channel.BasicConsumeAsync(_replyQueueName, true, consumer);
    }  // Fin StartAsync

    // Este es el que envía el mensaje y también recibe un cancellation token (cancelación no
    // implementada), y devuelve una promesa de resultado string.  Es la función local que hace
    // el cálculo, retorna una promesa, manda el cálculo a hacerse en el servidor, recbe el
    // resultado y lo asigna a la promesa. Recordar que puedo tener varias tareas que invocan
    // CallAsync con diferentes parámetros en forma asincrónica, incluso que vuelven a invocar
    // aún cuando todavía no recibieron el resultado de la anterior solicitud
    public async Task<string> CallAsync(string message,
        CancellationToken cancellationToken = default)
    {
        // verifico que el canal esté bien...
        if (_channel is null)
        {
            throw new InvalidOperationException();
        }
        
        // cada solicitud es identificada con ID y generamos el ID para la sesión ...
        string correlationId = Guid.NewGuid().ToString();
        
        // Le dice al servidor a dónde debe enviar la respuesta a la solicitud
        var props = new BasicProperties
        {
            CorrelationId = correlationId, // y qué sesión es ...
            ReplyTo = _replyQueueName // responder a esta cola "amq.gen-ABC123..."
        };
        
        // guardar la promesa de forma que cuando al respuesta vuelva, pueda ser completada.
        var tcs = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        _callbackMapper.TryAdd(correlationId, tcs);
        
        // Y envia la solicitud al servidor...
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: QUEUE_NAME,
            mandatory: true, basicProperties: props, body: messageBytes);
        
        // usamos cancellationToken para terminar si fuera necesario...
        // le ponemos una delegada para que lo cancele en forma asincrónica
        using CancellationTokenRegistration ctr =
            cancellationToken.Register(() =>
            {
                _callbackMapper.TryRemove(correlationId, out _);
                tcs.SetCanceled();
            });
        
        // esperamos a que esté el resultado para devolver un string
        return await tcs.Task;
    }
    
    // Terminar de forma prolija la conexión y el canal
    public async ValueTask DisposeAsync()
    {
        // si el canal está funcionando
        if (_channel is not null)
        {
            await _channel.CloseAsync();
        }
        
        // si la conexión está funcionando
        if (_connection is not null)
        {
            await _connection.CloseAsync();
        }
        // listo
    }
}
// Lo de arriba prepara el mecanismo de enviar solicitudes y de
// recibir resultados del servidor


public class Rpc
{
    // Esta es la aplicación de consola con la que interactuamos
    public static async Task Main(string[] args)
    {
        // hay que invocarla por línea de comando y pasarle un número n
        Console.WriteLine("RPC Client");
        // si no tiene parámetros ingresa un 30
        string n = args.Length > 0 ? args[0] : "30";
        // Le pedimos al servidor que nos resuelva n
        await InvokeAsync(n);

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    private static async Task InvokeAsync(string n)
    {
        // creamos un cliente RPC
        var rpcClient = new RpcClient();
        await rpcClient.StartAsync();
        // Llama el callasync() en el rpcClient e imprime el resultado.
        Console.WriteLine(" [x] Requesting fib({0})", n);
        var response = await rpcClient.CallAsync(n);
        Console.WriteLine(" [.] Got '{0}'", response);
    } // El dispose es automático
}