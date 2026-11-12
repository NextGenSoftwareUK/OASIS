using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.OLand;

public sealed class OLandQuoteRequest
{
    public List<string> ParcelIds { get; set; } = new();
    public string PaymentMethod { get; set; }
    public string ProviderType { get; set; }
    public string ReceivingWallet { get; set; }
    public string PayingWallet { get; set; }
    public string Email { get; set; }
    public string IntendedUse { get; set; }
}
public sealed class OLandPaymentRequest { public string TransactionId { get; set; } }
[BsonIgnoreExtraElements]
public sealed class OLandOrder
{
    public string Id { get; set; }
    public string AvatarId { get; set; }
    public string RequestId { get; set; }
    public string Fingerprint { get; set; }
    [BsonIgnoreIfNull] public string ActiveAvatar { get; set; }
    public List<string> ParcelIds { get; set; } = new();
    public long AmountMinorUsd { get; set; }
    public string Currency { get; set; } = "usd";
    public double AreaSquareMetres { get; set; }
    public string PaymentMethod { get; set; }
    public string ProviderType { get; set; }
    public string ReceivingWallet { get; set; }
    public string PayingWallet { get; set; }
    public string Email { get; set; }
    public string IntendedUse { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public string State { get; set; } = "quoted";
    public string StripeSessionId { get; set; }
    public string CheckoutUrl { get; set; }
    public string PaymentIdentity { get; set; }
    public string TreasuryAddress { get; set; }
    public string NativeAmountRaw { get; set; }
    public string NativeSymbol { get; set; }
    public decimal? NativeUsdPrice { get; set; }
    public string NativePriceAssetId { get; set; }
    public string NativePriceSource { get; set; }
    public string NativePriceVersion { get; set; }
    public DateTime? NativePricePublishedUtc { get; set; }
    public DateTime? NativePriceFetchedUtc { get; set; }
    public string NativeAmountUnit { get; set; }
    public int NativeDecimals { get; set; }
    public string NftStandard { get; set; }
    public string ChainId { get; set; }
    public string PaymentMemo { get; set; }
    public DateTime? PaidUtc { get; set; }
    public string MintState { get; set; } = "not_started";
    public string MintTransactionId { get; set; }
    public string MintMessage { get; set; }
}
[BsonIgnoreExtraElements]
public sealed class OLandParcelClaim
{
    public string Id { get; set; }
    public string OrderId { get; set; }
    public string AvatarId { get; set; }
    public bool Owned { get; set; }
}
public sealed class OLandCommerceException : Exception
{
    public string Code { get; }
    public OLandCommerceException(string code, string message) : base(message) { Code = code; }
}
public interface IOLandOrderStore
{
    Task<OLandOrder> Get(string id, CancellationToken token);
    Task<List<OLandOrder>> List(string avatar, string beforeOrderId, int limit, CancellationToken token);
    Task<List<OLandOrder>> PendingMint(CancellationToken token);
    Task<OLandOrder> FindRequest(string avatar, string request, CancellationToken token);
    Task<Dictionary<string, OLandParcelClaim>> Claims(IReadOnlyList<string> parcels, CancellationToken token);
    Task<OLandOrder> Reserve(OLandOrder order, CancellationToken token);
    Task<bool> Transition(string id, string from, string to, CancellationToken token);
    Task AttachStripe(string id, string session, string url, CancellationToken token);
    Task<OLandOrder> GrantPaid(string id, string paymentIdentity, DateTime paidUtc, CancellationToken token);
    Task Expire(string id, string expectedState, CancellationToken token);
    Task<bool> BeginMint(string id, CancellationToken token);
    Task CompleteMint(string id, string transactionId, string message, CancellationToken token);
}
public interface IOLandPaymentGateway
{
    bool StripeAvailable { get; }
    Task<(string SessionId, string Url)> CreateStripe(OLandOrder order, CancellationToken token);
    Task<(string Status, string PaymentIdentity, DateTime PaidUtc)> VerifyStripe(OLandOrder order, CancellationToken token);
    Task PrepareNative(OLandOrder order, CancellationToken token);
    Task<(string PaymentIdentity, DateTime PaidUtc)> VerifyNative(OLandOrder order, string transaction, CancellationToken token);
}
public interface IOLandNftIssuer
{
    Task<(string TransactionId, string Message)> Mint(OLandOrder order, CancellationToken token);
}
