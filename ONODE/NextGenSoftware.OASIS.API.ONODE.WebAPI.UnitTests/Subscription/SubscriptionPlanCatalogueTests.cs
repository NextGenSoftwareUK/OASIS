using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

public sealed class SubscriptionPlanCatalogueTests
{
    [Fact]
    public async Task AdvertisedPricesMatchStripeCurrencyAndAmountWithoutMutatingDefinitions()
    {
        var prices = new Mock<ISubscriptionPlanPriceReader>();
        var amounts = new Dictionary<string, decimal> { ["bronze"]=9, ["silver"]=29, ["gold"]=99 };
        prices.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((id, _) => Task.FromResult(new SubscriptionPlanPrice(amounts[id], "GBP", "month")));
        var catalogue = new SubscriptionPlanCatalogue(prices.Object);
        var result = await catalogue.GetAllAsync();
        result.Single(x => x.Id == "bronze").Currency.Should().Be("GBP");
        result.Single(x => x.Id == "gold").PriceMonthly.Should().Be(99);
        result.Single(x => x.Id == "free").PriceMonthly.Should().Be(0);
        prices.Verify(x => x.GetAsync("free", It.IsAny<CancellationToken>()), Times.Never);
        prices.Verify(x => x.GetAsync("enterprise", It.IsAny<CancellationToken>()), Times.Never);
        SubscriptionPlanCatalogue.Definitions.Single(x => x.Id == "bronze").Currency.Should().Be("USD");
        SubscriptionPlanCatalogue.Definitions.Single(x => x.Id == "bronze").PriceMonthly.Should().Be(0);
    }

    [Fact]
    public async Task UnavailableStripePricesDoNotProduceInventedPurchasablePlans()
    {
        var prices = new Mock<ISubscriptionPlanPriceReader>();
        prices.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Stripe is unavailable"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new SubscriptionPlanCatalogue(prices.Object).GetAllAsync());
    }

    [Theory]
    [InlineData("usd", 1234, 12.34)]
    [InlineData("gbp", 900, 9)]
    [InlineData("jpy", 1234, 1234)]
    [InlineData("kwd", 1234, 1.234)]
    public void StripeAmountsUseTheCurrencyMinorUnit(string currency, long minor, double expected) =>
        StripeAmount.FromMinorUnits(minor, currency).Should().Be(Convert.ToDecimal(expected));
}
