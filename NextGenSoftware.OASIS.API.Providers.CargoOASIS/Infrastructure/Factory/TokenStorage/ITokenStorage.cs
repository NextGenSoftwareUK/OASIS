using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage
{
    public interface ITokenStorage
    {
        Task SetToken(string token);
        Task<string> GetToken();
    }
}