using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace GrpcServer
{
    // Se definen el stub de las funciones que que se declararon en el contrato.
    // hereda de la clase base generada por el framework gRPC a partir del .proto
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger; 
        // les autogenera un logger, es opcional. se pueden ignorar si no lo necesitan
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }
        
        // implementación de SayHello recibe HelloRequest y devuelve HelloReply
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            // Genera una task que inmediatamente devuelve un HelloReply.
            return Task.FromResult(new HelloReply
            {
                Message = "Hola " + request.Name
            });
        }
        // El parámetro context en cada función (específicamente ServerCallContext
        // context) es un objeto de tipo ServerCallContext, que proporciona información
        // y controles relacionados con el contexto de la llamada gRPC. Para debugging,
        // terminar/abortar la comunicación, información del client,  etc.
        
        // Implementación de AddNumbers
        public override Task<NumbersReturn> AddNumbers(NumbersRequest request, ServerCallContext context)
        {
            return Task.FromResult(new NumbersReturn
            {
                Suma = request.NumeroA + request.NumeroB
            });
        }
        
        // Implementación de GetUserList
        public override Task<UserListReply> GetUserList(UserListRequest request, ServerCallContext context)
        {
            // devolvemos lista con {Juan 32 a�os, Maria 23 a�os}
            UserListReply listaDeUsuarios = new UserListReply();
            listaDeUsuarios.Users.Add(new User { Name = "Enrique", Age = 61, IsMale = true });
            listaDeUsuarios.Users.Add(new User { Name = "Karen", Age = 23, IsMale = false });
            listaDeUsuarios.Users.Add(new User { Name = "Gerardo", Age = 35, IsMale = true });
            // reply.MaxAge = 44;  // Opcional
            return Task.FromResult(listaDeUsuarios);
        }

        // Consejo: cuando especifiquen una nueva función en el .proto, al momento de implementarla
        // escribir .. public override .. y dejar que el IDE les autocomplete la función 
    }
}