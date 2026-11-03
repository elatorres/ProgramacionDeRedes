using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqAsyncExample.Shared;

namespace RabbitMqServer
{
    // Este es el servidor
    // basado en https://www.rabbitmq.com/tutorials/tutorial-six-dotnet
    class Program
    {
        static async Task Main(string[] args)
        {
            // Creamos una fábrica y ya la vinculamos con la url del servidor rabbitmq
            // usuario y password va por defecto guest.
            var factory = new ConnectionFactory { HostName = "localhost" };
            
            // Creamos la conexión y el canal
            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            // El nombre de la cola donde el servidor recibe los mensajes de solicitud es la siguiente:
            const string queueName = "request-queue";
            
            // Creamos la cola si no existe
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true, // <== nuevas versiones no permiten cola efímera
                exclusive: false, // Podemos tener varios servidores atendiendo la cola
                autoDelete: false, // Es persistente 
                arguments: null);

            // Limitamos la cantidad de mensajes sin acknowledge por consumidor para distribuir
            // el trabajo uniformemente.
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
            
            // Creamos el consumidor que va a atender a la cola.
            var consumer = new AsyncEventingBasicConsumer(channel);
            
            // Al consumidor le asignamos una tarea delegada para procesar los mensages en forma asincrónica.
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var body = ea.Body.ToArray(); // Extrae el cuerpo del mensaje
                var message = Encoding.UTF8.GetString(body); // lo convierte a cadena 
                RequestMessage requestMessage = null;  // Crea un objeto respuesta vacío
                // y le cargamos el contenido a la respuesta
                try
                {
                    requestMessage = JsonSerializer.Deserialize<RequestMessage>(message);
                }
                catch (JsonException ex) // Si hay error
                {
                    Console.WriteLine($"[Server] Error deserializing request: {ex.Message}");
                    // rechazamos el mensaje (potencialmente podría ir a una DLQ, no implementado aquí)
                    // Ver 05_MOM_02_DeadletterProducer y 05_MOM_02_DeadletterConsumer.
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }
                
                // Extraemos el nombre de la cola para responder.
                var replyTo = ea.BasicProperties?.ReplyTo;
                // Y el correlationId
                var correlationId = ea.BasicProperties?.CorrelationId;

                Console.WriteLine($"[Server] Received request: '{requestMessage?.Payload}' (CorrID: {correlationId})");

                // Simulate non-blocking async work
                await Task.Delay(1000); // Aquí se haría el trabajo requerido
                
                // Creamos la respuesta (Símplemente le agregamos "Procesado" al final)
                var responsePayload = $"{requestMessage?.Payload} processed";
                var responseMessage = new ResponseMessage(responsePayload);
                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(responseMessage));

                // Y le agregamos las propiedades a la respuesta/mensaje de vuelta, el correlationId
                var replyProps = new BasicProperties
                {
                    CorrelationId = correlationId
                };
                
                // SI la cola de retorno no es vacía (es correcta) mandamos el mensaje de vuelta
                if (!string.IsNullOrEmpty(replyTo))
                {
                    await channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: replyTo,
                        mandatory: false,
                        basicProperties: replyProps,
                        body: responseBytes);

                    Console.WriteLine($"[Server] Sent response: '{responsePayload}' to queue: '{replyTo}' (CorrID: {correlationId})");
                }
                
                // Y hacemos el acknowledge al mensaje recibido que ya fue procesado
                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }; // Fin de declaración del delegado de procesar mensajes
            
            // Le decimos al consumidor que empiece a consumir de la cola
            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            // , y quedamos esperando
            Console.WriteLine(" [x] Server awaiting requests");
            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        } // Fin Main
    }
}
