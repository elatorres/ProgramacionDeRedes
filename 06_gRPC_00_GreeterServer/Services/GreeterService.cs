using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


// el namespace del servidor de gRPC. Debe ser el mismo que el definido en el .proto
namespace Server_gRPC
{
    // Esta clase es la implementación que hereda de la clase abstracta
    // autogenerada Greeter.GreeterBase a partir del la descripción del
    // protocolo en el archivo .proto
    // Implementa los servicios declarados: SayHello, Sum y ObtenerListaUsuarios 
    public class GreeterService : Greeter.GreeterBase
    {
        // The constructor receives a logger through a dependency injection
        //ASP.NET Core inyecta las dependencias al crear este servicio.
        // En este caso, el servicio recibe un ILogger<GreeterService>
        // a través del constructor automáticamente. Este registrador/logger
        // se puede utilizar para registrar la actividad del servidor (logs),
        // las excepciones o las llamadas RPC (aunque no se utiliza en los métodos aquí).
        private readonly ILogger<GreeterService> _logger;
        // Se lo pasamos al constructor
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }
        
        // Este método implementa la función remota SayHello
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            // Log the operation
            _logger.LogInformation("Received SayHello request from {Peer} with name: {Name}", context.Peer, request.Name);
            
            // Devuelve el string "Hola " concatenado con el nombre del mensaje de HelloRequest
            // Perform operation
            var reply = new HelloReply { Message = "Hola " + request.Name };
            
            // return Task.FromResult(new HelloReply { Message = "Hola " + request.Name });
            
            // Log the result
            _logger.LogInformation("Responded to SayHello with message: {Message}", reply.Message);
            
            // Devuelve el resultado
            return Task.FromResult(reply);
        }
        
        // Este método implementa la función remota Sum.
        // Recibe una NumRequest conteniendo dos números, A y B, y devuelve la suma.
        public override Task<NumReply> Sum(NumRequest request, ServerCallContext context)
        {
            // habilito logging. Ver: appsettings.json
            _logger.LogInformation("Received Sum request from {Peer}: A={A}, B={B}",
                context.Peer, request.A, request.B);

            int result = request.A + request.B;

            _logger.LogInformation("Returning Sum result: {Result}", result);

            return Task.FromResult(new NumReply { Result = result });
        }
        
        // Este método implementa una solicitud por una lista de usuarios
        // Es un ejemplo de cómo obtener una lista de datos estructurados como respuesta
        public override Task<UsuListReply> ObtenerListaUsuarios(UsuRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Received ObtenerListaUsuarios request from {Peer}", context.Peer);
            
            // Invoca al método de CrearUsuarios
            var list = CrearUsuarios();

            _logger.LogInformation("Returning {Count} users", list.Usuarios.Count);

            return Task.FromResult(list);
        }

        private static UsuListReply CrearUsuarios()
        {
            // Crea una lista UsuListReply del tipo especificado en el archivo .proto
            var list = new UsuListReply();
            // Creates un par de objetos de la clase Usuario del tipo especificado en el archivo .proto
            var usu1 = new Usuario { Nombre = "Juan", Apellido = "Perez", Edad = 25};
            var usu2 = new Usuario { Nombre = "Maria", Apellido = "Garcia", Edad = 30 };
            // Agrega esos Usuarios a la UsuReplyList
            list.Usuarios.Add(usu1);
            list.Usuarios.Add(usu2);
            // y retorna la lists al cliente remoto
            return list;
        }
    }
}
