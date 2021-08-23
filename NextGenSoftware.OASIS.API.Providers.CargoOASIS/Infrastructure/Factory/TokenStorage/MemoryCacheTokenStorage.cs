using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage
{
    public class MemoryCacheTokenStorage : ITokenStorage
    {
        private readonly IMemoryCache _memoryCache;
        private readonly string _tokenKey;
        public MemoryCacheTokenStorage()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _tokenKey = "_token";
        }

        public async Task SetTaken(string token)
        {
            await Task.Run(() =>
            {
                _memoryCache.Set(_tokenKey, token);
            });
        }

        public Task<string> GetToken()
        {
            throw new System.NotImplementedException();
        }
    }
}