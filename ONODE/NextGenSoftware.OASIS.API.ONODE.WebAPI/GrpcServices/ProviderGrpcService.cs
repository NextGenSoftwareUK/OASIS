using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class ProviderGrpcService : ProviderService.ProviderServiceBase
    {
        public override Task<StringResponse> GetCurrentStorageProviderType(ProviderEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var pt = ProviderManager.Instance.CurrentStorageProviderType.Value;
                return Task.FromResult(new StringResponse { Value = pt.ToString() });
            }
            catch (Exception ex) { return Task.FromResult(new StringResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<StringListResponse> GetAllRegisteredProviderTypes(ProviderEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var types = ProviderManager.Instance.GetAllRegisteredProviderTypes();
                var resp = new StringListResponse();
                if (types != null)
                    resp.Values.AddRange(types.Select(ev => ev.Value.ToString()));
                return Task.FromResult(resp);
            }
            catch (Exception ex) { return Task.FromResult(new StringListResponse { IsError = true, Message = ex.Message }); }
        }

        private static async Task ActivateProviderIfSpecifiedAsync(string providerType, bool setGlobally)
        {
            if (string.IsNullOrWhiteSpace(providerType)) return;
            if (!System.Enum.TryParse<ProviderType>(providerType, true, out var pt)) return;
            if (pt == ProviderType.Default || pt == ProviderType.None) return;
            await OASISBootLoader.OASISBootLoader.GetAndActivateStorageProviderAsync(pt, null, false, setGlobally);
        }

        private static T ParseEnum<T>(string value, T defaultValue) where T : struct, System.Enum
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return System.Enum.TryParse<T>(value, true, out var result) ? result : defaultValue;
        }
    }
}
