using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;

// RPC
// 
// La clase RpcClient implementa un canal bidireccional con el servidor donde se le pide una
// operación y se espera el retorno del resultado. Maneja las múltiples solicitudes y es
// reentrante. Esto es que puede tener nuevas solicitudes aunque no haya terminado de procesar
// la anteriores. Con el mensaje se envía un id para identificar la solicitud y también la cola
// privada de respuesta para el cliente.
// Cada cliente puede hacer múltiples solicitudes asincrónicas y múltiples clientes se asocian
// a diferentes colas de respuesta, po lo que el servidor puede atender a múltiples clientes
// en la misma máquina y/o en máquinas diferentes.

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
        // extrae el nombre autogenerado de la cola, para que lo puedan mandar paar el callback
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
    // el cálculo, retorna una promesa, manda el cálculo a hacerse en el servidor, recibe el
    // resultado y lo asigna a la promesa. Recordar que puedo tener varias tareas que invocan
    // CallAsync con diferentes parámetros en forma asincrónica, incluso que vuelven a invocar
    // aún cuando todavía no recibieron el resultado de la anterior solicitud
    // Modified CallAsync in RpcClient class
    public Task<string> CallAsync(string message, CancellationToken cancellationToken = default)
    {
        // verifico que el canal esté bien...
        if (_channel is null) throw new InvalidOperationException("Channel not initialized");
        // cada solicitud es identificada con ID y generamos el ID para la sesión ...
        string correlationId = Guid.NewGuid().ToString();
        // Le dice al servidor a dónde debe enviar la respuesta a la solicitud, a esta cola "amq.gen-ABC123..."
        // y qué sesión es a la que responde.
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = _replyQueueName  // <<==
        };
        // Creamos la "Promesa"
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        // y la agregamos al diccionario
        _callbackMapper.TryAdd(correlationId, tcs);
        // Registramos el cancellation token, si se cancela la tarea hay que eliminarla del diccionario
        // y poner la promesa como cancelada.
        // Habría que avisarle al servidor que cancele la tarea, pero esa ya es otra historia...
        cancellationToken.Register(() =>
        {
            _callbackMapper.TryRemove(correlationId, out _);
            tcs.TrySetCanceled();
        });

        // Manda y olvida la publicación. 
        var messageBytes = Encoding.UTF8.GetBytes(message);
        _ = _channel.BasicPublishAsync(
            exchange: string.Empty, // Exchange por defecto ""
            routingKey: QUEUE_NAME,  // Cola del servidor
            mandatory: true, 
            basicProperties: props, 
            body: messageBytes);

        // ===============================================
        // Devuelve la Task (la "Promesa") inmediatamente sin un await
        return tcs.Task;
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
        // la gracia está en invocar varios clientes de estos al mismo tiempo
        // y ver como cada uno recibe su respuesta en forma consistente
        Console.WriteLine("RPC Client");
        // Generamos cuatro números aleatorios
        Random r = new Random();
        int kInt = r.Next(3, 30);
        int lInt = r.Next(3, 30);
        int mInt = r.Next(3, 30);
        int nInt = r.Next(3, 30);
        
        // Los convertimos a string (simplemente porque partimos de un ejemplo que hacía eso)
        string k = kInt.ToString();
        string l = lInt.ToString();
        string m = mInt.ToString();
        string n = nInt.ToString();
        // Le pedimos al servidor que nos resuelva Fib para k, l, m y n.
        await InvokeAsync(k,l,m,n);
        // y teminamos
        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
    
    private static async Task InvokeAsync(string k, string l, string m, string n)
    {
        // 0. creamos un cliente RPC
        var rpcClient = new RpcClient();
        await rpcClient.StartAsync();

        // 1. Comenzamos los cuatro cálculos (sin await aún!) y cada uno nos devuelve una promesa.
        Console.WriteLine($" [x] Requesting fib({k})...");
        Task<string> fibTask1 = rpcClient.CallAsync(k);

        Console.WriteLine($" [x] Requesting fib({l})...");
        Task<string> fibTask2 = rpcClient.CallAsync(l);
        
        Console.WriteLine($" [x] Requesting fib({m})...");
        Task<string> fibTask3 = rpcClient.CallAsync(m);

        Console.WriteLine($" [x] Requesting fib({n})...");
        Task<string> fibTask4 = rpcClient.CallAsync(n);
        
        // ¡¡Las cuatro operaciones se están ejecutando al mismo tiempo en el servidor!!

        // 3. Hacemos otras cosas mientras esperamos que el servidor responda
        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine(" [.] Client is doing other background work...");
            await Task.Delay(500); 
        }

        // 4. Ahora cuando realmente necesitamos los resultados entonces hacemos el await de las promesas
        Console.WriteLine(" [!] Client now needs the results. Waiting...");
        // Si ya están los resultados en realidad no espera nada
        // y los imprime
        string result1 = await fibTask1;
        Console.WriteLine($" [.] Got result for fib({k}): {result1}");
        string result2 = await fibTask2;
        Console.WriteLine($" [.] Got result for fib({l}): {result2}");
        string result3 = await fibTask3;
        Console.WriteLine($" [.] Got result for fib({m}): {result3}");
        string result4 = await fibTask4;
        Console.WriteLine($" [.] Got result for fib({n}): {result4}");
        // El dispose es automático al salir
    }
}