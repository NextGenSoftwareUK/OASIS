using Microsoft.Extensions.Configuration;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.OLand;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Stripe.Checkout;

public sealed class OLandPaymentEvidenceTests
{
    [Theory]
    [InlineData("amount")]
    [InlineData("currency")]
    [InlineData("owner")]
    [InlineData("order")]
    [InlineData("fingerprint")]
    [InlineData("session")]
    public void StripeReceiptMustMatchEveryImmutableOrderField(string changed)
    {
        var order = new OLandOrder { Id="order", AvatarId="avatar", AmountMinorUsd=1700, Fingerprint="hash", StripeSessionId="cs_order" };
        var session = new Session { Id="cs_order", Mode="payment", Currency="usd", AmountTotal=1700, ClientReferenceId="order",
            Metadata=new() { ["oland_order_id"]="order", ["oland_avatar_id"]="avatar", ["oland_fingerprint"]="hash" } };
        OLandPaymentGateway.ValidateStripe(order, session);
        switch (changed)
        {
            case "amount": session.AmountTotal=1699; break;
            case "currency": session.Currency="eur"; break;
            case "owner": session.Metadata["oland_avatar_id"]="other"; break;
            case "order": session.Metadata["oland_order_id"]="other"; break;
            case "fingerprint": session.Metadata["oland_fingerprint"]="other"; break;
            case "session": session.Id="cs_other"; break;
        }
        Assert.Throws<OLandCommerceException>(() => OLandPaymentGateway.ValidateStripe(order, session));
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("wrong-chain")]
    [InlineData("unfinalized")]
    [InlineData("failed")]
    [InlineData("wrong-memo")]
    [InlineData("wrong-amount")]
    [InlineData("wrong-sender")]
    [InlineData("wrong-recipient")]
    [InlineData("duplicate-transfers")]
    [InlineData("expired")]
    public async Task NativeReceiptNeedsExactFinalizedAppliedTransfer(string scenario)
    {
        var utc=DateTime.UtcNow;
        var order=new OLandOrder { Id="order", ProviderType="SolanaOASIS", ChainId="devnet-genesis", NativeSymbol="SOL",
            NativeAmountUnit="lamports", NativeDecimals=9, NativeAmountRaw="100000000", PayingWallet="buyer", TreasuryAddress="treasury",
            PaymentMemo="oland:order", CreatedUtc=utc.AddMinutes(-1), ExpiresUtc=utc.AddMinutes(1) };
        var transfers=new List<NativePaymentTransfer> { new(scenario=="wrong-sender"?"other":"buyer",
            scenario=="wrong-recipient"?"other":"treasury", scenario=="wrong-amount"?"99999999":"100000000", "lamports","SOL") };
        if(scenario=="duplicate-transfers")transfers.Add(transfers[0]);
        var receipt=new NativePaymentReceipt { ProviderType=ProviderType.SolanaOASIS, ChainId=scenario=="wrong-chain"?"mainnet-genesis":"devnet-genesis",
            TransactionId="signature", Finalized=scenario!="unfinalized", Succeeded=scenario!="failed",
            CreatedUtc=scenario=="expired"?utc.AddMinutes(2):utc, Memo=scenario=="wrong-memo"?"oland:other":"oland:order", Transfers=transfers };
        var provider=new Mock<IOASISProvider>();
        provider.SetupGet(p=>p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SolanaOASIS));
        provider.As<IOASISPaymentReceiptProvider>().Setup(p=>p.GetPaymentReceiptAsync("signature",It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<NativePaymentReceipt>(receipt));
        var settings=new OLandCommerceSettings { Chains=new() { ["SolanaOASIS"]=new() { ChainId="devnet-genesis",TreasuryAddress="treasury",
            NativeSymbol="SOL",NativeAmountUnit="lamports",NativeDecimals=9,NativePaymentEvidenceEnabled=true } } };
        var gateway=new OLandPaymentGateway(settings,new ConfigurationBuilder().Build(),()=>new[]{provider.Object},Mock.Of<IAssetUsdQuoteSource>());
        if(scenario=="valid")Assert.Equal("chain:SolanaOASIS:devnet-genesis:signature",(await gateway.VerifyNative(order,"signature",default)).PaymentIdentity);
        else await Assert.ThrowsAsync<OLandCommerceException>(()=>gateway.VerifyNative(order,"signature",default));
    }
}
