using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec
{
    public interface IAztecService
    {
        Task<PrivateNote> CreatePrivateNoteAsync(decimal value, string ownerPublicKey, string metadata = null);
        Task<AztecProof> GenerateProofAsync(string proofType, object payload);
        Task<AztecTransaction> SubmitProofAsync(AztecProof proof);
        Task<PrivateNote> NullifyNoteAsync(string noteId, AztecProof proof);
        
        // Stablecoin-specific methods
        Task<OASISResult<string>> MintStablecoinAsync(string aztecAddress, decimal amount, string zcashTxHash, string viewingKey);
        Task<OASISResult<string>> BurnStablecoinAsync(string aztecAddress, decimal amount, string positionId);
        Task<OASISResult<string>> DeployToYieldStrategyAsync(string aztecAddress, decimal amount, string strategy);
        Task<OASISResult<string>> SeizeCollateralAsync(string aztecAddress, decimal amount);
    }
}

