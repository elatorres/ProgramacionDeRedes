using Grpc.Core;
using System.Threading.Tasks;

namespace GrpcDivisionExample
{
    public class CalculatorServiceImpl : CalculatorService.CalculatorServiceBase
    {
        public override Task<DivideReply> Divide(DivideRequest request, ServerCallContext context)
        {
            // Check for division by zero exception condition
            if (request.Denominator == 0)
            {
                throw new RpcException(new Status(
                    StatusCode.InvalidArgument, 
                    "Division by zero is not allowed."
                ));
            }

            double result = request.Numerator / request.Denominator;

            return Task.FromResult(new DivideReply
            {
                Result = result
            });
        }
    }
}