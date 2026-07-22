using Grpc.Core;
using NextGenSoftware.OASIS.STAR.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
namespace NextGenSoftware.OASIS.STAR.WebAPI.GrpcServices
{
    public class StarGrpcService : StarService.StarServiceBase
    {
        private static STARAPI? _starAPI;
        private static readonly object _lock = new object();

        private static STARAPI GetStarAPI()
        {
            if (_starAPI == null)
            {
                lock (_lock)
                {
                    if (_starAPI == null)
                        _starAPI = new STARAPI(new STARDNA());
                }
            }
            return _starAPI;
        }

        public override Task<StarStatusResponse> GetStatus(StarEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var starAPI = GetStarAPI();
                var isIgnited = starAPI.IsOASISBooted;
                return Task.FromResult(new StarStatusResponse
                {
                    IsError = false,
                    Message = "OK",
                    IsIgnited = isIgnited,
                    Status = isIgnited ? "Ignited" : "Not Ignited"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new StarStatusResponse { IsError = true, Message = ex.Message });
            }
        }

        public override async Task<StarGenericResponse> Ignite(IgniteMsg request, ServerCallContext context)
        {
            try
            {
                var starAPI = GetStarAPI();
                var result = await starAPI.BootOASISAsync(
                    string.IsNullOrWhiteSpace(request.Username) ? "admin" : request.Username,
                    string.IsNullOrWhiteSpace(request.Password) ? "admin" : request.Password);
                return new StarGenericResponse
                {
                    IsError = result.IsError,
                    Message = result.Message ?? (result.IsError ? "Failed to ignite STAR." : "STAR ignited successfully.")
                };
            }
            catch (Exception ex)
            {
                return new StarGenericResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<StarGenericResponse> Extinguish(StarEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var result = await STARAPI.ShutdownOASISAsync();
                return new StarGenericResponse
                {
                    IsError = result.IsError,
                    Message = result.Message ?? (result.IsError ? "Failed to extinguish STAR." : "STAR extinguished successfully.")
                };
            }
            catch (Exception ex)
            {
                return Task.FromResult(new StarGenericResponse { IsError = true, Message = ex.Message }).Result;
            }
        }

        public override Task<HealthResponse> GetHealth(StarEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var starAPI = GetStarAPI();
                var isBooted = starAPI.IsOASISBooted;
                return Task.FromResult(new HealthResponse
                {
                    IsError = false,
                    Message = "OK",
                    Status = isBooted ? "Healthy" : "Degraded",
                    Service = "Web5 STAR ODK gRPC",
                    Version = "2.0.0"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new HealthResponse { IsError = true, Message = ex.Message });
            }
        }
    }
}
