using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    //public interface INftService
    //{
    //    Task<OASISResult<NftTransactionRespone>> CreateNftTransaction(CreateNftTransactionRequest request);
    //    Task<OASISResult<int>> GetOlandPrice(int count, string couponCode);
    //    Task<OASISResult<PurchaseOlandResponse>> PurchaseOland(PurchaseOlandRequest request);
    //}

    //public enum NftProvider
    //{
    //    Cargo = 1,
    //    Solana = 2
    //}

    //public class CreateNftTransactionRequest
    //{
    //    public NftProvider NftProvider { get; set; }
    //    public string MintWalletAddress { get; set; }
    //    public string FromWalletAddress { get; set; }
    //    public string ToWalletAddress { get; set; }
    //    public int Amount { get; set; }
    //    public string MemoText { get; set; }

    //    //public ExchangeTokenRequest SolanaExchange { get; set; }
    //    //public PurchaseRequestModel CargoExchange { get; set; }
    //}

    //public class NftTransactionRespone
    //{
    //    public string TransactionResult { get; set; }
    //}

    //public class PurchaseOlandResponse
    //{
    //    public string PurchaseResult { get; set; }

    //    public PurchaseOlandResponse(string purchaseResult)
    //    {
    //        PurchaseResult = purchaseResult;
    //    }
    //}

    //public class PurchaseOlandRequest
    //{
    //    public Guid OlandId { get; set; }
    //    public Guid AvatarId { get; set; }
    //    public string AvatarUsername { get; set; }
    //    public string Tiles { get; set; }
    //    public string WalletAddress { get; set; }
    //    public string CargoSaleId { get; set; }
    //}
}