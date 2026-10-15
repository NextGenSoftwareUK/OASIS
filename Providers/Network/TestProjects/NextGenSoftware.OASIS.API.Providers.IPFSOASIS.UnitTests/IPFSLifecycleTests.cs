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
    [Fact]
    public void PublicationFailureCapturesImmutableCompleteReceipts()
    {
        var receipts=new List<string> {"avatar-cid","detail-cid"};
        var cause=new System.IO.IOException("owned failure");
        var error=new IPFSRecordPublicationException(receipts,cause);
        receipts.Clear();
        Assert.Equal(new[] {"avatar-cid","detail-cid"},error.AcknowledgedRecordCids);
        Assert.Same(cause,error.InnerException); Assert.Equal("avatar-cid",error.OriginalTransactionId);
        Assert.Throws<NotSupportedException>(()=>((IList<string>)error.AcknowledgedRecordCids).Add("foreign"));
    }

    [Fact]
    public async Task AcknowledgedRecordIndexFailureFencesActualV2Failover()
    {
        var provider=new IPFSOASIS(Settings("http://127.0.0.1:1"));
        var cause=new System.IO.IOException("owned index encryption failure after record acknowledgement");
        typeof(IPFSOASIS).GetField("_storageKeyResolver",BindingFlags.Instance|BindingFlags.NonPublic)!
            .SetValue(provider,new Func<byte[]>(()=>throw cause));
        var method=typeof(IPFSOASIS).GetMethod("PublishAcknowledgedRecordAsync",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var task=(Task)method.Invoke(provider,new object[] {"acknowledged-record-cid",new HolonResume {Id=Guid.NewGuid(),RecordKind="holon"}})!;
        var error=await Assert.ThrowsAsync<IPFSRecordPublicationException>(()=>task);
        Assert.Equal("acknowledged-record-cid",error.OriginalTransactionId); Assert.Same(cause,error.InnerException);
        var router=typeof(NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive);
        var shouldFailOver=router.GetMethod("ShouldFailOver",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(bool));
        var result=new NextGenSoftware.OASIS.Common.OASISResult<bool> {IsError=true,Exception=error};
        Assert.False((bool)shouldFailOver.Invoke(null,new object[] {new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {Operation="SaveHolon"},result})!);
    }

    [Theory]
    [InlineData("avatar")]
    [InlineData("avatar-detail")]
    [InlineData("holon")]
    public void PrivateEntityEncryptionBindsKindAndGuid(string kind)
    {
        var provider=new IPFSOASIS(Settings("http://127.0.0.1:1"));
        var key=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32); StorageKey(provider,()=> (byte[])key.Clone());
        var id=Guid.NewGuid(); var json="{\"Id\":\""+id+"\",\"Email\":\"owned@example.invalid\"}";
        var protect=typeof(IPFSOASIS).GetMethod("ProtectRecord",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var unprotect=typeof(IPFSOASIS).GetMethod("UnprotectRecord",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var envelope=(string)protect.Invoke(provider,new object[] {kind,id,json})!;
        Assert.DoesNotContain("owned@example.invalid",envelope); Assert.DoesNotContain(id.ToString(),envelope);
        Assert.Equal(json,unprotect.Invoke(provider,new object[] {kind,id,envelope}));
        Assert.IsAssignableFrom<System.Security.Cryptography.CryptographicException>(Assert.Throws<TargetInvocationException>(()=>unprotect.Invoke(provider,new object[] {kind,Guid.NewGuid(),envelope})).InnerException);
        var other=kind=="avatar" ? "holon":"avatar";
        Assert.IsAssignableFrom<System.Security.Cryptography.CryptographicException>(Assert.Throws<TargetInvocationException>(()=>unprotect.Invoke(provider,new object[] {other,id,envelope})).InnerException);
        Assert.IsAssignableFrom<System.Security.Cryptography.CryptographicException>(Assert.Throws<TargetInvocationException>(()=>unprotect.Invoke(provider,new object[] {kind,id,json})).InnerException);
        StorageKey(provider,()=>System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        Assert.IsAssignableFrom<System.Security.Cryptography.CryptographicException>(Assert.Throws<TargetInvocationException>(()=>unprotect.Invoke(provider,new object[] {kind,id,envelope})).InnerException);
    }

    [Theory]
    [InlineData("avatar")]
    [InlineData("avatar-detail")]
    [InlineData("holon")]
    public async Task EntitySaveWithMissingKeyCannotPublishPrivateBlob(string kind)
    {
        var dna=Settings("http://127.0.0.1:1"); var provider=new IPFSOASIS(dna);
        StorageKey(provider,()=>throw new InvalidOperationException("Owned missing entity key"));
        Func<Task> save=kind=="avatar" ? async()=> {await provider.SaveAvatarToFile(new NextGenSoftware.OASIS.API.Core.Holons.Avatar {Id=Guid.NewGuid()});}
            :kind=="avatar-detail" ? async()=> {await provider.SaveAvatarDetailToFile(new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail {Id=Guid.NewGuid()});}
            :async()=> {await provider.SaveHolonToFile(new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()});};
        var error=await Assert.ThrowsAsync<InvalidOperationException>(save);
        Assert.Equal("Owned missing entity key",error.Message); Assert.Null(provider.IPFSClient);
        Assert.Null(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
    }

    [Theory]
    [InlineData("avatar")]
    [InlineData("avatar-detail")]
    [InlineData("holon")]
    public void SameGuidKindsKeepIndependentCurrentIndexEntries(string kind)
    {
        var provider=new IPFSOASIS(Settings("http://127.0.0.1:1")); var id=Guid.NewGuid();
        var entries=new Dictionary<string,HolonResume>();
        foreach(var value in new[] {"avatar","avatar-detail","holon"}) entries.Add(value+"-cid",new HolonResume {Id=id,RecordKind=value});
        typeof(IPFSOASIS).GetField("_idLookup",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,entries);
        var find=typeof(IPFSOASIS).GetMethod("FindIndexEntry",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var selected=(KeyValuePair<string,HolonResume>)find.Invoke(provider,new object[] {kind,(Func<HolonResume,bool>)(row=>row.Id==id),false})!;
        Assert.Equal(kind+"-cid",selected.Key);
        var replace=typeof(IPFSOASIS).GetMethod("ReplaceIndexEntry",BindingFlags.Instance|BindingFlags.NonPublic)!;
        replace.Invoke(provider,new object[] {"replacement-cid",new HolonResume {Id=id,RecordKind=kind}});
        Assert.Equal(3,entries.Count); Assert.False(entries.ContainsKey(kind+"-cid")); Assert.Equal(kind,entries["replacement-cid"].RecordKind);
        foreach(var other in new[] {"avatar","avatar-detail","holon"}) if(other!=kind) Assert.True(entries.ContainsKey(other+"-cid"));
    }

    [Fact]
    public void AmbiguousIndexSelectionAndConflictingCidCannotSilentlyReplaceRecords()
    {
        var provider=new IPFSOASIS(Settings("http://127.0.0.1:1")); var id=Guid.NewGuid();
        var entries=new Dictionary<string,HolonResume> {{"first",new HolonResume {Id=id,RecordKind="avatar"}},{"second",new HolonResume {Id=id,RecordKind="avatar"}}};
        typeof(IPFSOASIS).GetField("_idLookup",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,entries);
        var find=typeof(IPFSOASIS).GetMethod("FindIndexEntry",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Assert.IsType<NextGenSoftware.OASIS.Common.StorageIdentityAmbiguousException>(Assert.Throws<TargetInvocationException>(()=>find.Invoke(provider,new object[] {"avatar",(Func<HolonResume,bool>)(row=>row.Id==id),false})).InnerException);
        entries["second"]=new HolonResume {Id=id,RecordKind="avatar-detail"};
        var replace=typeof(IPFSOASIS).GetMethod("ReplaceIndexEntry",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(()=>replace.Invoke(provider,new object[] {"second",new HolonResume {Id=id,RecordKind="avatar"}})).InnerException);
        Assert.Equal(2,entries.Count); Assert.Equal("avatar",entries["first"].RecordKind); Assert.Equal("avatar-detail",entries["second"].RecordKind);
    }

    private static void StorageKey(IPFSOASIS provider,Func<byte[]> resolver)
        => typeof(IPFSOASIS).GetField("_storageKeyResolver",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,resolver);
    private static string TransformIndex(IPFSOASIS provider,string method,string value)
        => (string)typeof(IPFSOASIS).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(provider,new object[] {value})!;

    [Fact]
    public void PrivateIndexRoundTripDoesNotPublishSearchValuesInPlaintext()
    {
        var key=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var provider=new IPFSOASIS(Settings("http://127.0.0.1:1")); StorageKey(provider,()=> (byte[])key.Clone());
        var id=Guid.NewGuid();
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string,HolonResume> {{"owned-cid",new HolonResume {Id=id,login="actual-user",email="fixture@example.invalid"}}});
        var envelope=TransformIndex(provider,"ProtectIndex",json);
        Assert.StartsWith("oasis-aes256gcm-gzip-v2:",envelope);
        Assert.DoesNotContain("actual-user",envelope); Assert.DoesNotContain("fixture@example.invalid",envelope); Assert.DoesNotContain(id.ToString(),envelope);
        Assert.Equal(json,TransformIndex(provider,"UnprotectIndex",envelope));
    }

    [Theory]
    [InlineData("wrong-key")]
    [InlineData("plaintext")]
    [InlineData("corrupt")]
    public void PrivateIndexRejectsWrongKeyPlaintextAndMalformedEnvelope(string scenario)
    {
        var key=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var provider=new IPFSOASIS(Settings("http://127.0.0.1:1")); StorageKey(provider,()=> (byte[])key.Clone());
        var envelope=TransformIndex(provider,"ProtectIndex","{}");
        if(scenario=="wrong-key") StorageKey(provider,()=>System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        if(scenario=="plaintext") envelope="{}";
        if(scenario=="corrupt") envelope="oasis-aes256gcm-gzip-v2:bad";
        var error=Assert.Throws<TargetInvocationException>(()=>TransformIndex(provider,"UnprotectIndex",envelope));
        if(scenario=="corrupt") Assert.IsType<FormatException>(error.InnerException);
        else Assert.IsAssignableFrom<System.Security.Cryptography.CryptographicException>(error.InnerException);
    }

    [Fact]
    public async Task MissingIndexKeyFailsBeforeIpfsClientAccessOrRootMutation()
    {
        var dna=Settings("http://127.0.0.1:1"); var provider=new IPFSOASIS(dna);
        StorageKey(provider,()=>throw new InvalidOperationException("Owned missing storage key"));
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>provider.SaveLookupToFile(new Dictionary<string,HolonResume>()));
        Assert.Equal("Owned missing storage key",error.Message); Assert.Null(provider.IPFSClient);
        Assert.Null(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
    }

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
