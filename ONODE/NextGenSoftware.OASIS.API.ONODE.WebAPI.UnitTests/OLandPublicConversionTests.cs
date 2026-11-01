using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.OLand;

public sealed class OLandPublicConversionTests
{
    [Fact]
    public async Task ExistingOlandUsdPriceConvertsToExactLamportsWithoutAnApiKey()
    {
        var now=DateTime.UtcNow;
        using var handler=new PriceHandler(now);
        using var http=new HttpClient(handler);
        var source=CoinGeckoExchangeRateService.ForPublicMarketQuotes(http,utcNow:()=>now);
        var settings=new OLandCommerceSettings {Chains=new() { ["SolanaOASIS"]=new() {
            ChainId="devnet",TreasuryAddress="treasury",NativeSymbol="SOL",NativeAmountUnit="lamports",
            NativeDecimals=9,NativePaymentEvidenceEnabled=true } } };
        var gateway=new OLandPaymentGateway(settings,new ConfigurationBuilder().Build(),()=>Array.Empty<IOASISProvider>(),source);
        var order=new OLandOrder {Id="owned-order",AmountMinorUsd=1700,ProviderType="SolanaOASIS"};
        await gateway.PrepareNative(order,default);
        Assert.Equal(1700,order.AmountMinorUsd);
        Assert.Equal("170000000",order.NativeAmountRaw);
        Assert.Equal(100m,order.NativeUsdPrice);
        Assert.Equal("oland:owned-order",order.PaymentMemo);
        Assert.NotNull(order.NativePricePublishedUtc);
        Assert.Contains("coingecko.com",order.NativePriceSource);
        await source.GetUsdQuoteAsync("SOL");
        Assert.Equal(1,handler.Calls);
        Assert.False(handler.SentCredential);
    }

    [Theory]
    [InlineData(-3600,100,200)]
    [InlineData(3600,100,200)]
    [InlineData(0,0,200)]
    [InlineData(0,100,429)]
    public async Task PublicQuotesStillRejectStaleFutureInvalidAndRateLimitedResponses(int age,int price,int status)
    {
        var now=DateTime.UtcNow;
        using var handler=new PriceHandler(now.AddSeconds(age),price,(HttpStatusCode)status);
        using var http=new HttpClient(handler);
        var result=await CoinGeckoExchangeRateService.ForPublicMarketQuotes(http,utcNow:()=>now).GetUsdQuoteAsync("SOL");
        Assert.True(result.IsError);Assert.Null(result.Result);
    }

    [Fact]
    public async Task ExistingAuthenticatedBillingModeStillRequiresItsCredential()
    {
        using var handler=new PriceHandler(DateTime.UtcNow);
        using var http=new HttpClient(handler);
        var result=await new CoinGeckoExchangeRateService(http,"").GetUsdQuoteAsync("SOL");
        Assert.True(result.IsError);Assert.Equal(0,handler.Calls);
    }

    private sealed class PriceHandler(DateTime at,int price=100,HttpStatusCode status=HttpStatusCode.OK):HttpMessageHandler
    {
        public int Calls;public bool SentCredential;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;SentCredential=request.Headers.Contains("x-cg-demo-api-key")||request.Headers.Contains("x-cg-pro-api-key");
            return Task.FromResult(new HttpResponseMessage(status) {Content=new StringContent(
                "{\"solana\":{\"usd\":"+price+",\"last_updated_at\":"+new DateTimeOffset(at).ToUnixTimeSeconds()+"}}",Encoding.UTF8,"application/json")});
        }
    }
}
