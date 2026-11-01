using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.OLand;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.OLand;
using NextGenSoftware.Utilities;
using Xunit;

public sealed class OLandCommerceContractTests
{
    [Theory]
    [InlineData(56, 12, 32, 687071.44, 6210141.33)]
    [InlineData(-44, 174, 59, 740526.32, 5123750.87)]
    public void ProjectionMatchesIndependentProjPublishedExamples(double lat, double lon, int zone, double east, double north)
    {
        var p = OLandParcelGrid.Project(lat, lon, zone);
        Assert.InRange(Math.Abs(p.Easting - east), 0, .02);
        Assert.InRange(Math.Abs(p.Northing - north), 0, .02);
        var original = OLandParcelGrid.Unproject(p.Easting, p.Northing, zone, lat < 0);
        Assert.InRange(Math.Abs(original.Latitude - lat), 0, .0000001);
        Assert.InRange(Math.Abs(original.Longitude - lon), 0, .0000001);
    }
    [Theory]
    [InlineData(21.1619, -86.8515)] [InlineData(31.55033, 74.726138)]
    [InlineData(-33.8688, 151.2093)] [InlineData(51.5074, -.1278)]
    public void ParcelIsOneProjectedMetreAndIndependentOfView(double lat, double lon)
    {
        var cell = OLandParcelGrid.At(lat, lon);
        Assert.Equal(cell.Id, OLandParcelGrid.Parse(cell.Id).Id);
        Assert.Equal(1, cell.AreaSquareMetres);
        var lower = OLandParcelGrid.Project(cell.Boundary[0].Latitude, cell.Boundary[0].Longitude, cell.Zone);
        var upper = OLandParcelGrid.Project(cell.Boundary[2].Latitude, cell.Boundary[2].Longitude, cell.Zone);
        Assert.InRange(Math.Abs(upper.Easting - lower.Easting - 1), 0, .001);
        Assert.InRange(Math.Abs(upper.Northing - lower.Northing - 1), 0, .001);
        Assert.Contains(OLandParcelGrid.Around(lat, lon), p => p.Id == cell.Id);
    }
    [Fact] public void CanonicalIdsAndGridBoundsAreValidated()
    {
        var parcel = OLandParcelGrid.At(21.1619, -86.8515);
        Assert.Throws<ArgumentException>(() => OLandParcelGrid.Parse(parcel.Id.Replace(":16:", ":016:")));
        Assert.Throws<ArgumentOutOfRangeException>(() => OLandParcelGrid.At(double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => OLandParcelGrid.At(89, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => OLandParcelGrid.Around(20, 20, 33));
        var grid = OLandParcelGrid.Around(21.1619, -86.8515, 32);
        Assert.Equal(1024, grid.Count);
        Assert.Equal(1024, grid.Select(p => p.Id).Distinct().Count());
    }
    [Fact] public async Task QuoteUsesServerAreaAndPriceAndReplaysImmutableOrder()
    {
        var f = new Fixture();
        var body = f.Request(); var key = Guid.NewGuid().ToString("D");
        var order = await f.Service.Quote(f.Actor, key, body, default);
        Assert.Equal(1700, order.AmountMinorUsd); Assert.Equal(1, order.AreaSquareMetres);
        Assert.Equal(order.Id, (await f.Service.Quote(f.Actor, key, body, default)).Id);
        body.IntendedUse = "Different request";
        Assert.Equal("OLAND_CONFLICT", (await Assert.ThrowsAsync<OLandCommerceException>(() => f.Service.Quote(f.Actor, key, body, default))).Code);
        f.Issuer.Verify(x => x.Mint(It.IsAny<OLandOrder>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact] public async Task DuplicateParcelsAndUnlistedChainAreRejected()
    {
        var f = new Fixture(); var body = f.Request(); body.ParcelIds.Add(body.ParcelIds[0]);
        await Assert.ThrowsAsync<OLandCommerceException>(() => f.Service.Quote(f.Actor, Guid.NewGuid().ToString("D"), body, default));
        body = f.Request(); body.ProviderType = "MongoDBOASIS";
        await Assert.ThrowsAsync<OLandCommerceException>(() => f.Service.Quote(f.Actor, Guid.NewGuid().ToString("D"), body, default));
        Assert.Empty(f.Store.Orders);
    }
    [Fact] public async Task CompetingBuyersCannotReserveSameParcel()
    {
        var f = new Fixture();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            try { await f.Service.Quote(Guid.NewGuid(), Guid.NewGuid().ToString("D"), f.Request(), default); return true; }
            catch (OLandCommerceException) { return false; }
        }));
        Assert.Single(results.Where(x => x)); Assert.Single(f.Store.Held);
    }
    [Fact] public async Task PaidOrderCannotBeReadByAnotherAvatarAndUnpaidOrderCannotMint()
    {
        var f = new Fixture(); var order = await f.Service.Quote(f.Actor, Guid.NewGuid().ToString("D"), f.Request(), default);
        await Assert.ThrowsAsync<OLandCommerceException>(() => f.Service.Status(Guid.NewGuid(), order.Id, default));
        await Assert.ThrowsAsync<OLandCommerceException>(() => f.Service.Fulfil(f.Actor, order.Id, default));
        f.Issuer.Verify(x => x.Mint(It.IsAny<OLandOrder>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact] public async Task StripePendingDoesNotGrantOwnershipAndDuplicatePaidNotificationsDoNotMintTwice()
    {
        var f = new Fixture(); var order = await f.Service.Quote(f.Actor, Guid.NewGuid().ToString("D"), f.Request(), default);
        await f.Service.BeginCheckout(f.Actor, order.Id, default);
        await f.Service.Status(f.Actor, order.Id, default);
        Assert.Null(order.PaidUtc);
        f.Payments.Setup(p => p.VerifyStripe(It.IsAny<OLandOrder>(), It.IsAny<CancellationToken>())).ReturnsAsync(("paid", "stripe:cs_test_order", f.Now));
        await f.Service.StripeNotification(order.Id, "cs_test_order", default);
        await f.Service.StripeNotification(order.Id, "cs_test_order", default);
        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => f.Service.Fulfil(f.Actor, order.Id, default)));
        Assert.Equal("paid", order.State); Assert.Equal("minted", order.MintState);
        f.Issuer.Verify(p => p.Mint(It.IsAny<OLandOrder>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.All(f.Store.Held.Values, p => Assert.True(p.Owned));
    }
    [Fact] public async Task InitiatedPaymentCannotReleaseReservationWithoutReconciliation()
    {
        var f = new Fixture(); var order = await f.Service.Quote(f.Actor, Guid.NewGuid().ToString("D"), f.Request(), default);
        await f.Service.BeginCheckout(f.Actor, order.Id, default);
        await Assert.ThrowsAsync<OLandCommerceException>(() => f.Service.CancelQuote(f.Actor, order.Id, default));
        Assert.Single(f.Store.Held);
    }
    [Fact] public async Task AmbiguousMintSubmissionIsNotRetried()
    {
        var f = new Fixture(); var order = await f.Service.Quote(f.Actor, Guid.NewGuid().ToString("D"), f.Request(), default);
        await f.Service.BeginCheckout(f.Actor, order.Id, default);
        f.Payments.Setup(p => p.VerifyStripe(It.IsAny<OLandOrder>(), It.IsAny<CancellationToken>())).ReturnsAsync(("paid", "stripe:cs_test_order", f.Now));
        await f.Service.Status(f.Actor, order.Id, default);
        f.Issuer.Setup(p => p.Mint(It.IsAny<OLandOrder>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TimeoutException("No definite submission outcome"));
        await Assert.ThrowsAsync<TimeoutException>(() => f.Service.Fulfil(f.Actor, order.Id, default));
        await f.Service.Fulfil(f.Actor, order.Id, default);
        Assert.Equal("needs_reconciliation", order.MintState);
        f.Issuer.Verify(p => p.Mint(It.IsAny<OLandOrder>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] public async Task StatusExpiresAnUntouchedQuoteAndReleasesItsParcels()
    {
        var f = new Fixture();
        var order = await f.Service.Quote(f.Actor, Guid.NewGuid().ToString("D"), f.Request(), default);
        order.ExpiresUtc = f.Now.AddSeconds(-1);
        var status = await f.Service.Status(f.Actor, order.Id, default);
        Assert.Equal("expired", status.State);
        Assert.Null(status.ActiveAvatar);
        Assert.Empty(f.Store.Held);
        f.Payments.Verify(p => p.VerifyStripe(It.IsAny<OLandOrder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] public async Task PublicNftMetadataIsPublishedOnlyForPaidOwnershipAndExcludesPrivateFormDetails()
    {
        var f=new Fixture();var order=await f.Service.Quote(f.Actor,Guid.NewGuid().ToString("D"),f.Request(),default);
        Assert.Equal("SPL",order.NftStandard);
        await Assert.ThrowsAsync<OLandCommerceException>(()=>f.Service.PublicNftMetadata(order.Id,default));
        await f.Service.BeginCheckout(f.Actor,order.Id,default);
        f.Payments.Setup(p=>p.VerifyStripe(It.IsAny<OLandOrder>(),It.IsAny<CancellationToken>())).ReturnsAsync(("paid","stripe:cs_test_order",f.Now));
        await f.Service.Status(f.Actor,order.Id,default);
        string json=System.Text.Json.JsonSerializer.Serialize(await f.Service.PublicNftMetadata(order.Id,default));
        Assert.Contains(order.ParcelIds[0],json);
        Assert.DoesNotContain(order.Email,json);Assert.DoesNotContain(order.IntendedUse,json);Assert.DoesNotContain(order.AvatarId,json);
    }

    private sealed class Fixture
    {
        public readonly Guid Actor = Guid.NewGuid(); public readonly DateTime Now = DateTime.UtcNow;
        public readonly FakeStore Store = new();
        public readonly Mock<IOLandPaymentGateway> Payments = new();
        public readonly Mock<IOLandNftIssuer> Issuer = new();
        public readonly OLandCommerceService Service;
        public Fixture()
        {
            var provider = new Mock<IOASISNFTProvider>();
            provider.SetupGet(p => p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SolanaOASIS));
            provider.SetupGet(p => p.ProviderCapabilities).Returns(new List<EnumValue<ProviderCategory>> { new(ProviderCategory.Blockchain), new(ProviderCategory.NFT) });
            Payments.SetupGet(p => p.StripeAvailable).Returns(true);
            Payments.Setup(p => p.CreateStripe(It.IsAny<OLandOrder>(), It.IsAny<CancellationToken>())).ReturnsAsync(("cs_test_order", "https://checkout.stripe.com/test"));
            Payments.Setup(p => p.VerifyStripe(It.IsAny<OLandOrder>(), It.IsAny<CancellationToken>())).ReturnsAsync(("pending", (string)null, default(DateTime)));
            Issuer.Setup(p => p.Mint(It.IsAny<OLandOrder>(), It.IsAny<CancellationToken>())).ReturnsAsync(("native-mint-test", "Minted test fixture"));
            Service = new(new OLandCommerceSettings { Enabled = true, PublicApiBaseUrl = "https://example.com/api", Chains = new() { ["SolanaOASIS"] = new() { ChainId = "test-genesis", NftStandard = "SPL" } } },
                Store, Payments.Object, Issuer.Object, count => Task.FromResult(count * 17), () => new[] { provider.Object }, () => Now);
        }
        public OLandQuoteRequest Request() => new() { ParcelIds = new() { OLandParcelGrid.At(21.1619, -86.8515).Id },
            PaymentMethod = "stripe", ProviderType = "SolanaOASIS", ReceivingWallet = "test-wallet", Email = "buyer@example.com", IntendedUse = "Plant a virtual garden" };
    }
    // Isolated test double: production uses Mongo snapshot/majority transactions and unique indexes.
    private sealed class FakeStore : IOLandOrderStore
    {
        public readonly Dictionary<string, OLandOrder> Orders = new(); public readonly Dictionary<string, OLandParcelClaim> Held = new();
        private readonly Dictionary<string, string> Receipts = new(); private readonly object gate = new();
        public Task<List<OLandOrder>> List(string actor,string before,int limit,CancellationToken t) {lock(gate)return Task.FromResult(Orders.Values.Where(o=>o.AvatarId==actor).Take(limit).ToList());}
        public Task<List<OLandOrder>> PendingMint(CancellationToken t) {lock(gate)return Task.FromResult(Orders.Values.Where(o=>o.State=="paid"&&o.MintState=="not_started").ToList());}
        public Task<OLandOrder> Get(string id, CancellationToken t) { lock(gate) return Task.FromResult(Orders.GetValueOrDefault(id)); }
        public Task<OLandOrder> FindRequest(string a, string r, CancellationToken t) { lock(gate) return Task.FromResult(Orders.Values.FirstOrDefault(o => o.AvatarId == a && o.RequestId == r)); }
        public Task<Dictionary<string, OLandParcelClaim>> Claims(IReadOnlyList<string> ids, CancellationToken t) { lock(gate) return Task.FromResult(Held.Where(p => ids.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value)); }
        public Task<OLandOrder> Reserve(OLandOrder o, CancellationToken t) { lock(gate) {
            if(o.ParcelIds.Any(Held.ContainsKey) || Orders.Values.Any(x => x.ActiveAvatar == o.AvatarId)) throw new OLandCommerceException("OLAND_CONFLICT", "Held");
            Orders.Add(o.Id,o); foreach(var id in o.ParcelIds) Held[id]=new(){Id=id,OrderId=o.Id,AvatarId=o.AvatarId}; return Task.FromResult(o); } }
        public Task<bool> Transition(string id,string from,string to,CancellationToken t) { lock(gate) {if(Orders[id].State!=from)return Task.FromResult(false);Orders[id].State=to;return Task.FromResult(true);} }
        public Task AttachStripe(string id,string session,string url,CancellationToken t) { lock(gate){Orders[id].StripeSessionId=session;Orders[id].CheckoutUrl=url;return Task.CompletedTask;} }
        public Task<OLandOrder> GrantPaid(string id,string payment,DateTime utc,CancellationToken t) {lock(gate){var o=Orders[id];if(Receipts.TryGetValue(payment,out var other)&&other!=id)throw new OLandCommerceException("OLAND_CONFLICT","Reused payment");Receipts[payment]=id;o.State="paid";o.PaidUtc=utc;o.PaymentIdentity=payment;o.ActiveAvatar=null;foreach(var p in Held.Values.Where(x=>x.OrderId==id))p.Owned=true;return Task.FromResult(o);} }
        public Task Expire(string id,string expected,CancellationToken t){lock(gate){if(Orders[id].State==expected){Orders[id].State="expired";Orders[id].ActiveAvatar=null;foreach(var p in Held.Values.Where(x=>x.OrderId==id).ToList())Held.Remove(p.Id);}return Task.CompletedTask;} }
        public Task<bool> BeginMint(string id,CancellationToken t){lock(gate){var o=Orders[id];if(o.MintState!="not_started")return Task.FromResult(false);o.MintState="submitting";return Task.FromResult(true);} }
        public Task CompleteMint(string id,string tx,string message,CancellationToken t){lock(gate){Orders[id].MintState=tx==null?"needs_reconciliation":"minted";Orders[id].MintTransactionId=tx;return Task.CompletedTask;} }
    }
}
