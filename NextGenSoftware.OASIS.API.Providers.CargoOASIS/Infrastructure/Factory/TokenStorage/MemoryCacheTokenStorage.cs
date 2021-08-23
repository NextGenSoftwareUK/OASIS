using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Exceptions;

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

        public async Task<string> GetToken()
        {
            return await Task.Run(() =>
            {
                var token = _memoryCache.Get(_tokenKey);
                if (token == null)
                    throw new UserNotRegisteredException();
                return token.ToString();
            });
        }
    }
}