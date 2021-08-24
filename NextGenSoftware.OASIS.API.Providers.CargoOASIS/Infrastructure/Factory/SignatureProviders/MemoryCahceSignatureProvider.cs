using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Exceptions;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.ConfigurationProvider;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders
{
    public class MemoryCacheSignatureProvider : ISignatureProvider
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IConfigurationProvider _configuration;
        private readonly ITokenStorage _tokenStorage;
        private readonly string _key;

        public MemoryCacheSignatureProvider()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _configuration = ConfigurationFactory.GetLocalStorageConfigurationProvider();
            _tokenStorage = TokenStorageFactory.GetMemoryCacheTokenStorage();
            _key = "_signature";
        }

        public async Task<(bool, string)> GetSignature()
        {
            try
            {
                var singingMessage = await _configuration.GetKey("singingMessage");
                var token = await _tokenStorage.GetToken();
            
                var signature = _memoryCache.Get(_key);
                if (signature != null) return (false, signature.ToString());
                signature = "dfdf";
                _memoryCache.Set(_key, signature);
                return (false, signature.ToString());
            }
            catch (UserNotRegisteredException e)
            {
                return (true, e.Message);
            }
            catch (Exception e)
            {
                return (true, e.Message);
            }
        }
    }
}