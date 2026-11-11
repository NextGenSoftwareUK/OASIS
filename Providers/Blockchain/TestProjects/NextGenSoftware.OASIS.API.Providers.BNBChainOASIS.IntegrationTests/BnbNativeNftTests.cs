using System.Globalization;
using System.Numerics;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.BNBChainOASIS.IntegrationTests;

public sealed class BnbNativeNftTests
{
    [BnbNativeNftFact]
    public async Task MintsTransfersAndBurnsOnBnbSmartChain()
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var fixture = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_BNB_CI_FIXTURE")!);
        Assert.Equal(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(fixture));
        Assert.StartsWith("oasis-bnb.", Path.GetFileName(fixture));

        const string rpc = "http://127.0.0.1:19845";
        const int chainId = 714;
        var owner = new Account(Environment.GetEnvironmentVariable("OASIS_BNB_CI_KEY")!, chainId);
        var recipient = new Account(Environment.GetEnvironmentVariable("OASIS_BNB_CI_NFT_RECIPIENT_KEY")!, chainId);
        var address = Environment.GetEnvironmentVariable("OASIS_BNB_CI_CONTRACT_NFT")!;
        var provider = new BNBChainOASIS(rpc, owner.PrivateKey, address, chainId);
        var activation = await provider.ActivateProviderAsync();
        Assert.True(!activation.IsError && activation.Result, activation.Message);

        const string readAbi = """
            [
              {"type":"function","name":"ownerOf","inputs":[{"name":"id","type":"uint256"}],"outputs":[{"name":"","type":"address"}],"stateMutability":"view"},
              {"type":"function","name":"balanceOf","inputs":[{"name":"owner","type":"address"}],"outputs":[{"name":"","type":"uint256"}],"stateMutability":"view"}
            ]
            """;
        var native = new Web3(rpc).Eth.GetContract(readAbi, address);
        var ownerOf = native.GetFunction("ownerOf");
        var balanceOf = native.GetFunction("balanceOf");
        Assert.Equal(BigInteger.Zero, await balanceOf.CallAsync<BigInteger>(owner.Address));
        Assert.Equal(BigInteger.Zero, await balanceOf.CallAsync<BigInteger>(recipient.Address));

        var minted = await provider.MintNFTAsync(new MintWeb3NFTRequest
        {
            Title = "BNB CI NFT",
            CollectionPublicKey = address,
            SendToAddressAfterMinting = owner.Address
        });
        Assert.True(!minted.IsError && minted.IsSaved, minted.Message);
        Assert.NotNull(minted.Result?.Web3NFT);
        var token = minted.Result.Web3NFT.TokenId;
        var tokenId = BigInteger.Parse(token, CultureInfo.InvariantCulture);
        Assert.Equal(owner.Address, await ownerOf.CallAsync<string>(tokenId), ignoreCase: true);

        var collection = await provider.LoadOnChainNFTDataAsync(address);
        Assert.True(!collection.IsError && collection.IsLoaded, collection.Message);
        Assert.Equal("Web3CoreOASIS", collection.Result.Title);

        var sent = await provider.SendNFTAsync(new SendWeb3NFTRequest
        {
            TokenId = token, TokenAddress = address, Amount = 1,
            FromWalletAddress = owner.Address, ToWalletAddress = recipient.Address
        });
        Assert.True(!sent.IsError && sent.IsSaved, sent.Message);
        Assert.Equal(recipient.Address, await ownerOf.CallAsync<string>(tokenId), ignoreCase: true);

        var burn = new BurnWeb3NFTRequest { TokenId = token, NFTTokenAddress = address, OwnerPrivateKey = owner.PrivateKey };
        var rejected = await provider.BurnNFTAsync(burn);
        Assert.True(rejected.IsError);
        Assert.Equal(recipient.Address, await ownerOf.CallAsync<string>(tokenId), ignoreCase: true);
        burn.OwnerPrivateKey = recipient.PrivateKey;
        var burned = await provider.BurnNFTAsync(burn);
        Assert.True(!burned.IsError && burned.IsDeleted, burned.Message);
        Assert.Equal(BigInteger.Zero, await balanceOf.CallAsync<BigInteger>(owner.Address));
        Assert.Equal(BigInteger.Zero, await balanceOf.CallAsync<BigInteger>(recipient.Address));
    }
}

public sealed class BnbNativeNftFactAttribute : FactAttribute
{
    public BnbNativeNftFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("BNB_NFT_ONLY") != "true")
            Skip = "Requires the disposable BNB Smart Chain NFT fixture.";
    }
}
