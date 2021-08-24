using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders
{
    public class MemoryCacheSignatureProvider : ISignatureProvider
    {
        private readonly IMemoryCache _memoryCache;
        private readonly string _key;
        private readonly string _singingMessage;

        public MemoryCacheSignatureProvider()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _key = "_signature";
        }

        public async Task<string> GetSignature()
        {
            return await Task.Run(() =>
            {
                var token = _memoryCache.Get(_key);
                if (token != null) return token.ToString();
                token = "dfdf";
                _memoryCache.Set(_key, token);
                return token.ToString();
            });        
        }
    }
}