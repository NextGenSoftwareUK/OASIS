using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class NftBurnExecutionTests
{
    public class ProviderBoundary : DispatchProxy
    {
        public int Calls;
        public Func<Task<OASISResult<IWeb3NFTTransactionResponse>>> Execute=null!;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            Assert.Equal("BurnNFTAsync",method!.Name);Calls++;
            return Execute();
        }
    }

    private sealed class Uncertain : Exception, INativeMutationReconciliationRequired
    {
        public string OriginalTransactionId => "owned-controlled-outcome";
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public async Task MutationExecutesOnceAndPreservesOutcome(int outcome)
    {
        var provider=DispatchProxy.Create<IOASISNFTProvider,ProviderBoundary>();
        var boundary=(ProviderBoundary)(object)provider;
        var cause=new Uncertain();
        var expected=new OASISResult<IWeb3NFTTransactionResponse> {
            Result=new Web3NFTTransactionResponse(),IsSaved=outcome==0,
            IsError=outcome==1,Exception=outcome==1 ? cause:null,
            IsWarning=outcome==4,Message="owned controlled provider result"};
        boundary.Execute=()=>outcome switch {
            2=>throw cause,
            3=>Task.FromResult<OASISResult<IWeb3NFTTransactionResponse>>(null!),
            _=>Task.FromResult(expected)};
        var request=new BurnWeb3NFTRequest {OwnerPrivateKey="",OwnerPublicKey="",OwnerSeedPhrase="",
            WaitTillNFTBurnt=true,WaitForNFTToBurnInSeconds=180,AttemptToBurnEveryXSeconds=0};
        var actual=await NftBurnExecution.ExecuteOnceAsync(provider,request);
        Assert.Equal(1,boundary.Calls);
        if(outcome is 0 or 1 or 4) Assert.Same(expected,actual);
        else Assert.True(actual.IsError);
        if(outcome is 1 or 2) Assert.Same(cause,actual.Exception);
    }
}
