using Grpc.Core;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GrpcAuthExample
{
    public class AuthServiceImpl : AuthService.AuthServiceBase
    {
        private readonly ConcurrentDictionary<string, string> _tokens = new();

        public override Task<AuthResponse> Authenticate(AuthRequest request, ServerCallContext context)
        {
            if (request.Username == "user" && request.Password == "pass")
            {
                string token = Guid.NewGuid().ToString();
                _tokens[token] = request.Username;

                return Task.FromResult(new AuthResponse
                {
                    Token = token,
                    Message = "Authentication successful."
                });
            }

            return Task.FromResult(new AuthResponse
            {
                Token = "",
                Message = "Authentication failed."
            });
        }

        public override Task<DataResponse> GetData(DataRequest request, ServerCallContext context)
        {
            // Extract token either from HTTP Authorization header or from request body
            string token = GetTokenFromContextOrRequest(context, request.Token);

            if (!string.IsNullOrEmpty(token) && _tokens.TryGetValue(token, out var username))
            {
                return Task.FromResult(new DataResponse
                {
                    Data = $"Hello, {username}! Here is your data."
                });
            }

            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid token."));
        }

        public override Task<FilteredDataResponse> GetFilteredData(FilteredDataRequest request, ServerCallContext context)
        {
            string token = GetTokenFromContextOrRequest(context, request.Token);

            if (!string.IsNullOrEmpty(token) && _tokens.TryGetValue(token, out var username))
            {
                string result = $"Filtered data for {username}: {request.FilterString}-{request.FilterNumber}";
                return Task.FromResult(new FilteredDataResponse
                {
                    Result = result
                });
            }

            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid token."));
        }

        private static string GetTokenFromContextOrRequest(ServerCallContext context, string bodyToken)
        {
            // 1. Check Authorization header first (Bearer <token>)
            var authHeader = context.RequestHeaders.GetValue("authorization");
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }

            // 2. Fall back to body token
            return bodyToken;
        }
    }
}