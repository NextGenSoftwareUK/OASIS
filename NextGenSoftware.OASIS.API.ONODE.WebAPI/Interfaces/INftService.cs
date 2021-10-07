using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    public interface INftService
    {
        Task<OASISResult<NftTransactionRespone>> CreateNftTransaction(CreateNftTransactionRequest request);
        Task<OASISResult<int>> GetOlandPrice(int count, string couponCode);
    }

    public enum NftProvider
    {
        Cargo = 1,
        Solana = 2
    }

    public class CreateNftTransactionRequest
    {
        public NftProvider NftProvider { get; set; }
        public ExchangeTokenRequest SolanaExchange { get; set; }
        public PurchaseRequestModel CargoExchange { get; set; }
    }

    public class NftTransactionRespone
    {
        public string TransactionResult { get; set; }
    }
}