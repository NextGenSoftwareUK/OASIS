using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders
{
    public interface ISignatureProvider
    {
        Task<string> GetSignature();
    }
}