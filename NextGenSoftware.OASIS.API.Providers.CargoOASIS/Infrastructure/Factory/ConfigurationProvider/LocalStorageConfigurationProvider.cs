using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.ConfigurationProvider
{
    public class LocalStorageConfigurationProvider : IConfigurationProvider
    {
        public async Task SetKey(string key, object value)
        {
            throw new System.NotImplementedException();
        }

        public async Task<object> GetKey(string key)
        {
            throw new System.NotImplementedException();
        }
    }
}