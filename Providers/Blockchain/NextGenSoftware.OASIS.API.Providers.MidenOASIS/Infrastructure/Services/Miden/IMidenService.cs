using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Models;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS.Infrastructure.Services.Miden
{
    public interface IMidenService
    {
        Task<PrivateNote> CreatePrivateNoteAsync(decimal value, string ownerPublicKey, string assetId = null, string metadata = null);
        Task<STARKProof> GenerateSTARKProofAsync(string programHash, object inputs, object outputs);
        Task<bool> VerifySTARKProofAsync(STARKProof proof);
        Task<PrivateNote> NullifyNoteAsync(string noteId, STARKProof proof);
        
        // Bridge-specific methods for Zcash ↔ Miden
        Task<OASISResult<string>> MintOnMidenAsync(string midenAddress, decimal amount, string zcashTxHash, string viewingKey);
        Task<OASISResult<string>> LockOnMidenAsync(string midenAddress, decimal amount, string zcashAddress);
        Task<OASISResult<string>> ReleaseFromMidenAsync(string midenAddress, decimal amount, string zcashAddress);
    }
}

