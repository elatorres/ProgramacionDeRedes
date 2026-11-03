using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqAsyncExample.Shared;

namespace RabbitMqClient
{
    // Este es el cliente
    // basado en https://www.rabbitmq.com/tutorials/tutorial-six-dotnet
    public class RabbitMqRpcClient : IAsyncDisposable, IDisposable
    {
        // declaramos la conexión, el canal, la cola de callback,
        // diccionario para gestionar los resultados con las promesas
        // y la cola de solicitud al servidor.
        private IConnection _connection;
        private IChannel _channel;
        private string _replyQueueName;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ResponseMessage>> _pendingMessages = new();
        private const string RequestQueueName = "request-queue";

        // Constructor/fabrica. Recibe la dirección IP, asume usuario y password igual a guest.
        // devuelve un cliente
        public static async Task<RabbitMqRpcClient> CreateAsync(string hostName = "localhost")
        {
            // crea un cliente
            var client = new RabbitMqRpcClient();
            // lo inicializa
            await client.InitializeAsync(hostName);
            // y lo devuelve
            return client;
        }

        private async Task InitializeAsync(string hostName)
        {
            // Crea una fábrica
            var factory = new ConnectionFactory { HostName = hostName };
            // Crea la conexión y el canal
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // QueueDeclareAsync devuelve QueueDeclareOk con QueueName
            var declareResult = await _channel.QueueDeclareAsync(
                queue: string.Empty, // <== cola sin nombre pues devuelve el nombre autogenerado
                exclusive: true,  // Es exclusiva de este cliente
                autoDelete: true, // efímera, se destruye al desconectarse
                arguments: null);
            
            // obtenemos el nombre autogenerado
            _replyQueueName = declareResult.QueueName;
            
            // Creamos un consumidor para procesar los mensajes recibidos
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceivedAsync; // Esta es la delegada que los va a procesar
            
            // Asignamos el consumidor a la cola  de nombre autogenerado
            await _channel.BasicConsumeAsync(
                queue: _replyQueueName,
                autoAck: true,
                consumer: consumer);
        } // Fin InitializeAsync
        
        // Esta es la función delegada que recibe los mensajes y los procesa
        private Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray(); // extrae el cuerpo
            var responseJson = Encoding.UTF8.GetString(body); // deserializa
            ResponseMessage responseMessage;  // Arma una respuesta
            // Trata de llenar la respuesta con el contenido recibido
            try
            {
                responseMessage = JsonSerializer.Deserialize<ResponseMessage>(responseJson);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[Client] Error deserializing response: {ex.Message}. Raw response: {responseJson}");
                return Task.CompletedTask; // Aviso que terminó
            }
            // Extraigo el correlationId
            var correlationId = ea.BasicProperties?.CorrelationId;
            // Si hay correlationId y hay promesa guardada la extraigo del diccionario
            if (!string.IsNullOrEmpty(correlationId) && _pendingMessages.TryRemove(correlationId, out var tcs))
            {
                // Le asigno la respuesta a la promesa / TaskCompletionSource
                tcs.TrySetResult(responseMessage); // <== ACÁ SE ASIGNA EL RESULTADO
            }
            else // sino
            {
                Console.WriteLine($"[Client] Received unexpected message with CorrelationId: '{correlationId}'. Payload: {responseMessage?.Payload}");
            }
            // aviso que se completó este método
            return Task.CompletedTask;
            // Pero el resultado tcs (TaskCompletionSource) lo devolvió SendRequestAsync
        }
        
        // método SendRequestAsync con mensaje y timeout
        public async Task<ResponseMessage> SendRequestAsync(string messagePayload, TimeSpan timeout)
        {
            // creo un correlationId, mensaje y lo serializo a bytes
            var correlationId = Guid.NewGuid().ToString();
            var requestMessage = new RequestMessage(messagePayload);
            var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestMessage));
            
            // creo la promesa a devolver, sin el valor aún y con continuation asincrónica
            var tcs = new TaskCompletionSource<ResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            // y la agrego al diccionario de mensajes a resolver
            _pendingMessages.TryAdd(correlationId, tcs);
            
            // agrego al mensaje las propiedades del correlationId y de la cola de retorno (call-back)
            var props = new BasicProperties
            {
                CorrelationId = correlationId,
                ReplyTo = _replyQueueName
            };

            Console.WriteLine($"[Client] Sending request: '{messagePayload}' (CorrID: {correlationId}) to queue: {RequestQueueName}");
            
            // Publico el mensaje al servidor
            await _channel.BasicPublishAsync(
                exchange: string.Empty,  // Exchange por defecto
                routingKey: RequestQueueName,
                mandatory: false,
                basicProperties: props,
                body: messageBytes);
            
            // creo un cancellation token por tiempo
            using var cts = new CancellationTokenSource(timeout);
            // Se asigna la delegada a ejecutarse al timeout
            using (cts.Token.Register(() =>
            {
                // Si existe la saca del diccionarios de tareas a resolver el resultado 
                if (_pendingMessages.TryRemove(correlationId, out var pendingTcs))
                {
                    // la cancela
                    pendingTcs.TrySetCanceled();
                    Console.WriteLine($"[Client] Request '{messagePayload}' (CorrID: {correlationId}) timed out after {timeout.TotalSeconds} seconds.");
                }
            }, useSynchronizationContext: false))
                
            {
                // Y espero a devolver el TaskCompletionSource<ResponseMessage>
                return await tcs.Task; // Cuando esté completado continúo acá y termino!!
                // No puedo quitar el await porque como es un método asincrónico, lo que
                // devuelve debe ser del tipo ResponseMessage en vez de Task<ResponseMessage>.
                // Para ver otra forma de hacerlo referirse a los ejemplos
                // en 05_MOM_00_AsyncReqResClientNonAwaited y 05_MOM_00_AsyncReqResClientNonAwaited..
            }
        } // Fin SendRequestAsync(message, timeout)
        
        // método SendRequestAsync sólo con mensaje (y sin timeout)
        // devuelve una promesa que va a ser rellenada con el mensaje que devuelve el servidor.
        // Se invoca sin await.
        public Task<ResponseMessage> SendRequestAsync(string messagePayload)
        {
            // Es un criterio de buen diseño que se separe la responsabilidad
            // Este SendRequestAsync sólo maneja el timeout
            // timeout por defecto de 30 segundos
            // Pensar otras formas de manejar resultados que tardan demasiado
            // en volver o que directamente nunca llegan.
            // SendRequestAsync(message, timeout) devuelve la promesa del mensaje !!
            // que a su vez la devuelve SendRequestAsync(message) [Ver arriba]
            return SendRequestAsync(messagePayload, TimeSpan.FromSeconds(30));
        } // Fin SendRequestAsync(message)
        
        // Destructor
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_channel is not null)
                {
                    await _channel.CloseAsync();
                    await _channel.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Error closing channel: {ex.Message}");
            }

            try
            {
                if (_connection is not null)
                {
                    await _connection.CloseAsync();
                    await _connection.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Error closing connection: {ex.Message}");
            }
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    // Este es el programa principal del cliente
    class Program
    {
        static async Task Main(string[] args)
        {
            // COmienza y crea un cliente vacío
            Console.WriteLine(" [x] Client starting...");
            RabbitMqRpcClient rpcClient = null;

            try
            {
                // El método fábrica (Factory) encapsula al constructor
                rpcClient = await RabbitMqRpcClient.CreateAsync();
                
                // Manda la solicitud No.1  ==============================================
                Console.WriteLine("Client: Sending request 1...");
                // SendRequestAsync devuelve la promesa del mensaje que enviará el servidor
                Task<ResponseMessage> responseTask1 = rpcClient.SendRequestAsync("Hello Server (Request 1)");
                Console.WriteLine("Client: Request 1 sent. Client is now doing other work...");
                
                // Hace otras cosas  =====================================================
                Console.WriteLine("Client: Performing some calculations...");
                await Task.Delay(500);
                Console.WriteLine("Client: Still working on other things...");
                await Task.Delay(500);
                
                // Revisa si hay respuesta de la solicitud No.1  =========================
                Console.WriteLine("Client: Now checking for response 1 (awaiting)...");
                try
                {
                    ResponseMessage response1 = await responseTask1;
                    Console.WriteLine($"Client: Received response 1: '{response1.Payload}'");
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Client: Request 1 timed out.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client: Error receiving response 1: {ex.Message}");
                }
                // Fin de la solicitud No.1 ==============================================

                // Manda la solicitud No.2  ==============================================
                Console.WriteLine("\nClient: Sending request 2...");
                Task<ResponseMessage> responseTask2 = rpcClient.SendRequestAsync("Another payload (Request 2)");
                Console.WriteLine("Client: Request 2 sent. Doing more client work...");
                await Task.Delay(100);
                
                // Manda la solicitud No.3  ==============================================
                Console.WriteLine("Client: Sending request 3...");
                Task<ResponseMessage> responseTask3 = rpcClient.SendRequestAsync("Final payload (Request 3)");
                Console.WriteLine("Client: Request 3 sent.");
                
                // Hace otras cosas  =====================================================
                Console.WriteLine("Client: Performing some calculations too...");
                await Task.Delay(500);
                Console.WriteLine("Client: Still working on other things too...");
                await Task.Delay(500);

                // Revisa si hay respuesta de la solicitud No.2  =========================
                Console.WriteLine("Client: Now checking for response 2 (awaiting)...");
                try
                {
                    ResponseMessage response2 = await responseTask2;
                    Console.WriteLine($"Client: Received response 2: '{response2.Payload}'");
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Client: Request 2 timed out.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client: Error receiving response 2: {ex.Message}");
                }
                // Fin de la solicitud No.2 ==============================================

                // Revisa si hay respuesta de la solicitud No.3  =========================
                Console.WriteLine("Client: Now checking for response 3 (awaiting)...");
                try
                {
                    ResponseMessage response3 = await responseTask3;
                    Console.WriteLine($"Client: Received response 3: '{response3.Payload}'");
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Client: Request 3 timed out.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client: Error receiving response 3: {ex.Message}");
                }
                // Fin de la solicitud No.3 ==============================================
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Main program error: {ex.Message}");
            }
            finally
            {
                // Terminamos y cerramos
                if (rpcClient is not null)
                {
                    await rpcClient.DisposeAsync();
                }

                Console.WriteLine("\nClient: All operations complete. Press Enter to exit client.");
                Console.ReadLine();
            }
        }
    }
}