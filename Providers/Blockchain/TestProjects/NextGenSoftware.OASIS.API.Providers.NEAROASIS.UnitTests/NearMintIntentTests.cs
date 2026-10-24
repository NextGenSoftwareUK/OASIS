using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass,DoNotParallelize]
public class NearMintIntentTests
{
    private sealed class PausedActivation : NEAROASIS
    {
        public readonly TaskCompletionSource Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public PausedActivation():base("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-service-key"){}
        public override async Task<OASISResult<bool>> ActivateProviderAsync(){Entered.SetResult();await Release.Task;return new(true);}
    }
    private sealed class RejectedActivation : NEAROASIS
    {
        public int Calls;
        public RejectedActivation():base("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-service-key"){}
        public override Task<OASISResult<bool>> ActivateProviderAsync(){Calls++;return Task.FromResult(new OASISResult<bool>{IsError=true,Message="Controlled activation must not be reached"});}
    }

    [DataTestMethod,DataRow(false,false,false),DataRow(false,true,false),DataRow(true,false,false),DataRow(true,true,false),
        DataRow(false,false,true),DataRow(false,true,true),DataRow(true,false,true),DataRow(true,true,true)]
    public async Task MintKeepsRecipientExactUnitsAndMetadataAcrossActivation(bool synchronous,bool explicitRecipient,bool explicitContract)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");PausedActivation? provider=null;
        try{
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","mint-intent.mjs"));
            // SDK bridge selection is captured by the provider constructor.
            provider=new PausedActivation();
            var request=new MintWeb3TokenRequest{Symbol=(explicitRecipient?"owned-original-target":"owned-original-default")+(explicitContract?"-contract":""),Amount=1,
                TokenAddress=explicitContract?"external.near":null!,
                ToWalletAddress=explicitRecipient?"receiver.near":null!,Title="original title",Description="original description",
                MetaData=new(){{"zone","north"}},Tags=new(){"original"}};
            var pending=synchronous?Task.Run(()=>provider.MintToken(request)):provider.MintTokenAsync(request);
            await provider.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            request.Symbol="changed";request.Amount=2;request.ToWalletAddress="changed.near";
            request.TokenAddress="changed-contract.near";
            request.Title="changed";request.Description="changed";request.MetaData["zone"]="south";request.Tags.Add("changed");
            provider.Release.SetResult();var result=await pending.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);
            Assert.AreEqual("controlled-original-mint-transaction",result.Result.TransactionResult);
        }finally{provider?.Release.TrySetResult();Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",previous);}
    }

    [DataTestMethod,DataRow("0.000000001"),DataRow("1.000000001"),DataRow("0"),DataRow("-1")]
    public async Task InvalidPrecisionAndNonpositiveMintStopBeforeActivation(string amount)
    {
        var provider=new RejectedActivation();var result=await provider.MintTokenAsync(new MintWeb3TokenRequest{Symbol="owned",Amount=decimal.Parse(amount,CultureInfo.InvariantCulture)});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentException>(result.Exception);Assert.AreEqual(0,provider.Calls);
    }

    [DataTestMethod,DataRow(""),DataRow(" ")]
    public async Task ExplicitBlankRecipientCannotSilentlyBecomeServiceOwner(string recipient)
    {
        var provider=new RejectedActivation();var result=await provider.MintTokenAsync(new MintWeb3TokenRequest{Symbol="owned",Amount=1,ToWalletAddress=recipient});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentException>(result.Exception);Assert.AreEqual(0,provider.Calls);
    }

    [DataTestMethod,DataRow(""),DataRow(" ")]
    public async Task ExplicitBlankContractCannotSilentlyBecomeConfiguredContract(string contract)
    {
        var provider=new RejectedActivation();var result=await provider.MintTokenAsync(new MintWeb3TokenRequest{Symbol="owned",Amount=1,TokenAddress=contract});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentException>(result.Exception);Assert.AreEqual(0,provider.Calls);
    }
}
