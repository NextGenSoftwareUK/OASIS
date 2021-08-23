using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage
{
    public class MemoryCacheTokenStorage : ITokenStorage
    {
        public Task SetTaken(string token)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetToken(string token)
        {
            throw new System.NotImplementedException();
        }
    }
}