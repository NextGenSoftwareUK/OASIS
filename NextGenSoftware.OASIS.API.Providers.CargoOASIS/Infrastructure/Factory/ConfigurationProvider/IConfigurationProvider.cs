using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.ConfigurationProvider
{
    public interface IConfigurationProvider
    {
        Task SetKey(string key, object value);
        Task<object> GetKey(string key);
    }
}