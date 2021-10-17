using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders
{
    public interface ISignatureProvider
    {
        /// <summary>
        /// Gets or generate signature
        /// </summary>
        /// <returns>Boolean - is there an error (true - yes, false - no), string - signature, if there are an error string will contain error message</returns>
        Task<(bool, string)> GetSignature();
    }
}