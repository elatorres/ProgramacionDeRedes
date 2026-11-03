namespace RabbitMqAsyncExample.Shared
{
    // basado en https://www.rabbitmq.com/tutorials/tutorial-six-dotnet
    // Mensaje de solicitud
    public class RequestMessage
    {
        public string Payload { get; set; }

        public RequestMessage(string payload)
        {
            Payload = payload;
        }
    }

    // Mensaje de Respuesta
    public class ResponseMessage
    {
        public string Payload { get; set; }

        public ResponseMessage(string payload)
        {
            Payload = payload;
        }
    }
}