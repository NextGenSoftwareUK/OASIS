using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Security
{
    /// <summary>Applies <see cref="ApiSurfaceAuthorization"/> to every gRPC call.</summary>
    public sealed class OASISGrpcAuthorizationInterceptor : Interceptor
    {
        private static void Authorize(ServerCallContext context)
        {
            if (ApiSurfaceAuthorization.PublicGrpcMethods.Contains(context.Method)) return;
            var decision = ApiSurfaceAuthorization.RequireWizard(context.GetHttpContext());
            if (decision == ApiSurfaceAuthorization.Decision.Allowed) return;
            throw new RpcException(new Status(
                decision == ApiSurfaceAuthorization.Decision.Unauthenticated ? StatusCode.Unauthenticated : StatusCode.PermissionDenied,
                ApiSurfaceAuthorization.Message(decision)));
        }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            Authorize(context);
            return continuation(request, context);
        }

        public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            Authorize(context);
            return continuation(requestStream, context);
        }

        public override Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            Authorize(context);
            return continuation(request, responseStream, context);
        }

        public override Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            Authorize(context);
            return continuation(requestStream, responseStream, context);
        }
    }
}
