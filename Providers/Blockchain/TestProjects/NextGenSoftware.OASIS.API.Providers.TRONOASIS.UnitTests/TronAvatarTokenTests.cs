using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronAvatarTokenTests
{
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task NativeRuntimeRetainsConfiguredResolverAndOriginalFailureAcrossDeactivation(bool synchronous)
    {
        var cause=new System.IO.IOException("controlled signing source unavailable");
        var resolutions=0;
        var provider=new TRONOASIS(fullNodeGrpcEndpoint:"http://127.0.0.1:17689",
            solidityNodeGrpcEndpoint:"http://127.0.0.1:17689",genesisBlockId:new string('a',64),
            nativeSigningKeyResolver:address=>{Assert.Equal("sender",address);resolutions++;return Task.FromException<string>(cause);});
        for(var attempt=0;attempt<2;attempt++)
        {
            var result=synchronous?provider.SendTransaction("sender","recipient",1m,"")
                :await provider.SendTransactionAsync("sender","recipient",1m,"");
            Assert.True(result.IsError);Assert.Same(cause,result.Exception);Assert.Null(result.Result);
            Assert.False(provider.IsProviderActivated);
            Assert.False((await provider.DeActivateProviderAsync()).IsError);
        }
        Assert.Equal(2,resolutions);
    }

    private sealed class FailedActivation(Exception cause, bool throws) : TRONOASIS
    {
        public override Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> ActivateProviderAsync()
            => throws ? Task.FromException<NextGenSoftware.OASIS.Common.OASISResult<bool>>(cause)
                : Task.FromResult(new NextGenSoftware.OASIS.Common.OASISResult<bool>
                {
                    IsError = true, Exception = cause, Message = "controlled activation failure",
                    ErrorCode = "CONTROLLED_TRANSPORT", DetailedMessage = "controlled activation detail"
                });
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task RawTransferRejectsInvalidSunBeforeStorageActivation(bool synchronous, bool throws)
    {
        var cause = new System.IO.IOException("controlled transport unavailable");
        var provider = new FailedActivation(cause, throws);
        var amount = throws ? 0.0000001m : -1m;
        var result = synchronous ? provider.SendTransaction("sender", "recipient", amount, "memo")
            : await provider.SendTransactionAsync("sender", "recipient", amount, "memo");
        Assert.True(result.IsError);
        Assert.IsType<ArgumentOutOfRangeException>(result.Exception);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData("id", true)]
    [InlineData("id", false)]
    [InlineData("username", true)]
    [InlineData("username", false)]
    [InlineData("email", true)]
    [InlineData("email", false)]
    public async Task NativeAvatarRoutesRejectInvalidSunAmountBeforeWalletOrStorage(string selector, bool synchronous)
    {
        var provider = new TRONOASIS();
        var from = Guid.NewGuid(); var to = Guid.NewGuid();
        const decimal amount = 0.0000001m;
        var result = selector switch
        {
            "id" => synchronous ? provider.SendTransactionById(from, to, amount)
                : await provider.SendTransactionByIdAsync(from, to, amount),
            "username" => synchronous ? provider.SendTransactionByUsername("sender", "recipient", amount)
                : await provider.SendTransactionByUsernameAsync("sender", "recipient", amount),
            _ => synchronous ? provider.SendTransactionByEmail("sender@example.org", "recipient@example.org", amount)
                : await provider.SendTransactionByEmailAsync("sender@example.org", "recipient@example.org", amount)
        };
        Assert.True(result.IsError);
        Assert.IsType<ArgumentOutOfRangeException>(result.Exception);
        Assert.Contains("SUN units", result.Message);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TokenByIdRejectsMissingAvatarBeforeStorageActivation(bool synchronous)
    {
        var provider = new TRONOASIS();
        var recipient = Guid.NewGuid();
        var result = synchronous
            ? provider.SendTransactionById(Guid.Empty, recipient, 1m, "TRX")
            : await provider.SendTransactionByIdAsync(Guid.Empty, recipient, 1m, "TRX");
        Assert.True(result.IsError);
        Assert.IsType<ArgumentException>(result.Exception);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TokenByIdRejectsMissingTokenInsteadOfUsingConfiguredStorageContract(bool synchronous)
    {
        var provider = new TRONOASIS(contractAddress: "storage-is-not-the-requested-token");
        var sender = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var result = synchronous
            ? provider.SendTransactionById(sender, recipient, 1m, "")
            : await provider.SendTransactionByIdAsync(sender, recipient, 1m, "");
        Assert.True(result.IsError);
        Assert.IsType<ArgumentException>(result.Exception);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }
}
