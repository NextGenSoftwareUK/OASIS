using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Providers
{
    public interface ISignatureProvider
    {
        Task<string> GetSignature();
    }
}