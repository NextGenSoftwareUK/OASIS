using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using Ipfs.Http;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

public class IPFSLifecycleTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("false")]
    [InlineData("error")]
    [InlineData("success")]
    public void RootPublicationRequiresActualPointerPersistenceSuccess(string scenario)
    {
        var cause=new System.IO.IOException("Owned pointer persistence failure");
        NextGenSoftware.OASIS.Common.OASISResult<bool>? saved=scenario=="missing" ? null:new() {
            Result=scenario is "success" or "error",IsError=scenario=="error",Exception=scenario=="error" ? cause:null};
        var method=typeof(IPFSOASIS).GetMethod("EnsureIndexPublication",BindingFlags.Static|BindingFlags.NonPublic)!;
        if(scenario=="success") {method.Invoke(null,new object?[] {"acknowledged-index-cid","previous-index-cid",saved});return;}
        var error=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object?[] {"acknowledged-index-cid","previous-index-cid",saved}));
        var publication=Assert.IsType<IPFSIndexPublicationException>(error.InnerException);
        Assert.Equal("acknowledged-index-cid",publication.ProposedIndexCid); Assert.Equal("previous-index-cid",publication.PreviousIndexCid);
        Assert.True(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(publication));
        if(scenario=="error") Assert.Same(cause,publication.InnerException);
    }

    [Fact]
    public void LookupSerializationDoesNotRepublishLegacyPasswordField()
    {
        var old="{\"Id\":\""+Guid.NewGuid()+"\",\"login\":\"actual-user\",\"email\":\"fixture@example.invalid\",\"password\":\"owned-sensitive-fixture\"}";
        var entry=Newtonsoft.Json.JsonConvert.DeserializeObject<HolonResume>(old)!;
        var encoded=Newtonsoft.Json.JsonConvert.SerializeObject(entry);
        Assert.DoesNotContain("password",encoded,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("owned-sensitive-fixture",encoded);
        Assert.Equal("actual-user",entry.login); Assert.Equal("fixture@example.invalid",entry.email);
    }

    [Fact]
    public async Task OnlyAnUnconfiguredColdLookupCanInitializeEmpty()
    {
        var provider=new IPFSOASIS(Settings("http://127.0.0.1:1"));
        var first=await provider.LoadLookupToJson();
        Assert.Empty(first); Assert.Same(first,await provider.LoadLookupToJson());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LookupFailureCannotReplaceRetainedIndexWithEmpty(bool configuredRoot)
    {
        var dna=Settings("http://127.0.0.1:1");
        dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress=configuredRoot ? "configured-root":null;
        var provider=new IPFSOASIS(dna);
        var retained=new Dictionary<string,HolonResume> {{"retained-reference",new HolonResume {Id=Guid.NewGuid()}}};
        var field=typeof(IPFSOASIS).GetField("_idLookup",BindingFlags.Instance|BindingFlags.NonPublic)!;
        field.SetValue(provider,retained);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>provider.LoadLookupToJson());
        Assert.Same(retained,field.GetValue(provider)); Assert.Single(retained);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    public void InvalidLookupContentIsNeverAnEmptyIndex(string content)
    {
        var method=typeof(IPFSOASIS).GetMethod("DeserializeLookup",BindingFlags.Static|BindingFlags.NonPublic)!;
        var error=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[] {content}));
        if(content=="null") Assert.IsType<System.IO.InvalidDataException>(error.InnerException);
        else Assert.IsType<Newtonsoft.Json.JsonSerializationException>(error.InnerException);
    }

    private static OASISDNA Settings(string endpoint)
    {
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {StorageProviders=new StorageProviderSettings()}};
        dna.OASIS.StorageProviders.IPFSOASIS=new IPFSOASISSettings {ConnectionString=endpoint};
        return dna;
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public async Task DeactivationOnlyDisconnectsClientAndIsIdempotent(bool synchronous,bool existingClient)
    {
        var provider=new IPFSOASIS(Settings("http://127.0.0.1:1"));
        if(existingClient) provider.IPFSClient=new IpfsClient("http://127.0.0.1:1");
        var result=synchronous ? provider.DeActivateProvider():await provider.DeActivateProviderAsync();
        Assert.False(result.IsError); Assert.True(result.Result); Assert.False(provider.IsProviderActivated); Assert.Null(provider.IPFSClient);
        var repeated=synchronous ? provider.DeActivateProvider():await provider.DeActivateProviderAsync();
        Assert.False(repeated.IsError); Assert.True(repeated.Result);
    }

    [Fact]
    public async Task InvalidEndpointPreservesActivationCauseWithoutShuttingDownPreviousClient()
    {
        var provider=new IPFSOASIS(Settings("http://["));
        provider.IPFSClient=new IpfsClient("http://127.0.0.1:1");
        var result=await provider.ActivateProviderAsync();
        Assert.True(result.IsError); Assert.IsType<UriFormatException>(result.Exception);
        Assert.False(provider.IsProviderActivated); Assert.Null(provider.IPFSClient);
    }
}
