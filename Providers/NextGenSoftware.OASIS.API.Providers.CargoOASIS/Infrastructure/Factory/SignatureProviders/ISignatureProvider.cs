using NextGenSoftware.OASIS.Common;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders
{
    public interface ISignatureProvider
    {
        /// <summary>
        /// Gets or generate signature
        /// </summary>
        /// <returns>Boolean - is there an error (true - yes, false - no), first string message (status), second string - signature</returns>
        Task<OASISResult<string>> GetSignature(string address, string singingMessage, string privateKey, string hostUrl);
    }
}