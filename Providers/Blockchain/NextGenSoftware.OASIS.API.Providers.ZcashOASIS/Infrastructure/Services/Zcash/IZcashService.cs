using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash
{
    public interface IZcashService
    {
        Task<ShieldedTransaction> CreateShieldedTransactionAsync(string fromAddress, string toAddress, decimal amount, string memo = null);
        Task<ViewingKey> GenerateViewingKeyAsync(string address);
        Task<PartialNote> CreatePartialNoteAsync(decimal amount, int numberOfParts);
    }
}

