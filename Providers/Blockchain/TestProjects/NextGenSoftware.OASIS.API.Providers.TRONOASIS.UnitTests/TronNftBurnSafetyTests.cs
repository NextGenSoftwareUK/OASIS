using System.Numerics;
using System.Reflection;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNftBurnSafetyTests
{
    private sealed class ConsumerBurnRequest : Core.Objects.NFT.Requests.BurnWeb3NFTRequest,Core.Interfaces.NFT.Requests.IBurnWeb3NFTRequest
    {
        public string SelectedKey="";
        string Core.Interfaces.NFT.Requests.IBurnWeb3NFTRequest.OwnerPrivateKey {get=>SelectedKey;init=>SelectedKey=value;}
    }
    public class StopAtWallet : DispatchProxy
    {
        public string? ParsedOwner;
        public readonly Exception Cause=new IOException("controlled stop before node access");
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            Assert.Equal("ParseAddress",method!.Name);
            ParsedOwner=(string)args![0]!;
            throw Cause;
        }
    }
    public class NoTransactions : DispatchProxy
    {
        public int Calls;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {Calls++;throw new InvalidOperationException("No signing or transaction operation expected.");}
    }

    [Theory]
    [InlineData(false,false)] [InlineData(false,true)]
    [InlineData(true,false)] [InlineData(true,true)]
    public async Task BurnKeepsValidatedSignerAcrossRuntimeWait(bool invalidReplacement,bool checkPublicKey)
    {
        var originalKey=new string('1',64);
        var originalOwner=new TronNet.TronECKey(originalKey,TronNet.TronNetwork.MainNet).GetPublicAddress();
        var wallet=DispatchProxy.Create<TronNet.IWalletClient,StopAtWallet>();
        var transactions=DispatchProxy.Create<TronNet.ITransactionClient,NoTransactions>();
        var client=new TronNativeTransferClient(wallet,transactions,new string('a',64));
        var provider=new TRONOASIS(nativeTransfers:client);
        var gate=(SemaphoreSlim)typeof(TRONOASIS).GetField("_runtimeLifecycle",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(provider)!;
        var request=new ConsumerBurnRequest {NFTTokenAddress=originalOwner+"/1",OwnerPrivateKey=originalKey,SelectedKey=originalKey,
            OwnerPublicKey=checkPublicKey?Convert.ToHexString(new Nethereum.Signer.EthECKey(originalKey).GetPubKey()):"",OwnerSeedPhrase=""};
        await gate.WaitAsync();
        Task<NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.NFT.Responses.IWeb3NFTTransactionResponse>> pending;
        try {
            pending=provider.BurnNFTAsync(request);Assert.False(pending.IsCompleted);
            request.SelectedKey=invalidReplacement?"invalid replacement":new string('2',64);
            request.NFTTokenAddress="changed-contract/2";
        }finally{gate.Release();}
        var result=await pending;
        var observed=(StopAtWallet)(object)wallet;
        Assert.True(result.IsError);Assert.Null(result.Result);
        Assert.Same(observed.Cause,result.Exception);
        Assert.Equal(originalOwner,observed.ParsedOwner);
        Assert.Equal(0,((NoTransactions)(object)transactions).Calls);
        Assert.False((await provider.DeActivateProviderAsync()).IsError);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("9007199254740993")]
    public void BurnPreservesNativeTokenId(string token)
    {
        var id = BigInteger.Parse(token);
        var bytes = (byte[])typeof(TronNativeTransferClient).GetMethod("EncodeNftBurn", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { id })!;
        Assert.Equal(36, bytes.Length);
        Assert.Equal("42966C68", Convert.ToHexString(bytes.AsSpan(0, 4)));
        Assert.Equal(id, new BigInteger(bytes.AsSpan(4), isUnsigned: true, isBigEndian: true));
    }

    [Theory]
    [InlineData("contract")]
    [InlineData("contract/-1")]
    [InlineData("contract/1/2")]
    [InlineData("contract/00000000-0000-0000-0000-000000000000")]
    public void RejectsMissingOrNonNativeIdentity(string address)
    {
        var method = typeof(TronNativeTransferClient).GetMethod("ParseNftAddress", BindingFlags.NonPublic | BindingFlags.Static)!;
        var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { address }));
        Assert.IsType<ArgumentException>(error.InnerException);
    }

    [Fact]
    public async Task MissingTransferRecipientCannotBecomeBurn()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        await Assert.ThrowsAsync<ArgumentException>(() => runtime.NativeTransfers.SendNftAsync(
            "unused", null!, "unused", BigInteger.Zero, new string('1', 64)));
    }
}
