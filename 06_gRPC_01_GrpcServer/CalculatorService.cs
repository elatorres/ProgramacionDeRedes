using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace GrpcMultiplication
{
    public class CalculatorService : Calculator.CalculatorBase
    {
        public override async Task<MultiplyReply> Multiply(MultiplyRequest request, ServerCallContext context)
        {
            // Simular un poquito de trabajo
            await Task.Delay(10); 
            
            double product = request.Number1 * request.Number2;
            
            System.Console.WriteLine($">>>>>>>>>>> [Servidor] Recibió: {request.Number1} y {request.Number2}. Producto Calculado: {product}");

            return new MultiplyReply
            {
                Result = product
            };
        }
    }
}