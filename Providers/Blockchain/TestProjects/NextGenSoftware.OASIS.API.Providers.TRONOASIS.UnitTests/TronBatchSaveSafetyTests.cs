using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronBatchSaveSafetyTests
{
    private sealed class ControlledProvider : TRONOASIS
    {
        public int Calls;
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            Calls++;
            return Task.FromResult(holon?.Name == "confirmed"
                ? new OASISResult<IHolon> { Result = holon, IsSaved = true }
                : new OASISResult<IHolon> { IsError = true, Message = "controlled write rejection" });
        }
    }

    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 2)]
    public async Task PartialBatchRetainsConfirmedItemsAndReportsFailure(bool continueOnError, int calls)
    {
        var provider = new ControlledProvider();
        var confirmed = new Holon { Name = "confirmed" };
        var result = await provider.SaveHolonsAsync(new IHolon[] { confirmed, null!, new Holon() }, continueOnError: continueOnError);
        Assert.True(result.IsError);
        Assert.Same(confirmed, Assert.Single(result.Result));
        Assert.Equal(calls, provider.Calls);
        Assert.Contains("controlled write rejection", result.Message);
    }

    [Fact]
    public async Task ImportCannotReportSuccessWhenAllWritesFail()
    {
        var provider = new ControlledProvider();
        var result = await provider.ImportAsync(new IHolon[] { new Holon(), new Holon() });
        Assert.True(result.IsError);
        Assert.False(result.Result);
        Assert.Equal(2, provider.Calls);
    }

    [Fact]
    public async Task FullyConfirmedBatchSucceeds()
    {
        var provider = new ControlledProvider();
        var result = await provider.SaveHolonsAsync(new IHolon[] { new Holon { Name = "confirmed" } });
        Assert.False(result.IsError);
        Assert.Single(result.Result);
    }
}
