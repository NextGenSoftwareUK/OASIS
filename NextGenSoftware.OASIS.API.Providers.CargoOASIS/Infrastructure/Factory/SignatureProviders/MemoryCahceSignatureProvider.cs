using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.ConfigurationProvider;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders
{
    public class MemoryCacheSignatureProvider : ISignatureProvider
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IConfigurationProvider _configuration;
        private readonly string _key;

        public MemoryCacheSignatureProvider()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _configuration = ConfigurationFactory.GetLocalStorageConfigurationProvider();
            _key = "_signature";
        }

        public async Task<string> GetSignature()
        {
            var singingMessage = await _configuration.GetKey("singingMessage");
            var token = _memoryCache.Get(_key);
            if (token != null) return token.ToString();
            token = "dfdf";
            _memoryCache.Set(_key, token);
            return token.ToString();
        }
    }
}