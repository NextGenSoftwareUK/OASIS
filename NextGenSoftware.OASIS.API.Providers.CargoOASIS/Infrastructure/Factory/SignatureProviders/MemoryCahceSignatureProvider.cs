using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Exceptions;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders
{
    public class MemoryCacheSignatureProvider : ISignatureProvider
    {
        private readonly IMemoryCache _memoryCache;
        private readonly string _key;
        
        public MemoryCacheSignatureProvider()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _key = "_signature";
        }

        public async Task<string> GetSignature()
        {
            return await Task.Run(() =>
            {
                _memoryCache.Set(_key, token);
                var token = _memoryCache.Get(_key);
                if (token == null)
                    throw new UserNotRegisteredException();
                return token.ToString();
            });        
        }
    }
}