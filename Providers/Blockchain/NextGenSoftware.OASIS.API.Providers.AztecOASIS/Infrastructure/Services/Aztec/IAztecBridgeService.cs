using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec
{
    public interface IAztecBridgeService
    {
        Task<AztecTransaction> DepositFromZcashAsync(decimal amount, string zcashTxId, PrivateNote aztecNote);
        Task<AztecTransaction> WithdrawToZcashAsync(PrivateNote note, AztecProof proof, string destinationAddress);
        Task<AztecTransaction> SyncBridgeEventAsync(string eventId);
    }
}

