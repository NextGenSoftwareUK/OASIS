using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidPrivateRecordCodecTests
{
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)]
    public async Task EncryptedCurrentAndArchivedMetadataRemainExact(int kind)
    {
        using var codec=new SolidPrivateRecordCodec(Root,Key());
        const decimal precise=0.1234567890123456789012345678m;
        const string literalDate="2026-10-20T10:11:12.1234567+02:00";
        IHolonBase source=kind==0?new Avatar{Id=Guid.NewGuid()}:kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon{Id=Guid.NewGuid()};
        source.MetaData=new(){["precise"]=precise,["literal_date"]=literalDate};
        async Task Check<T>(T row) where T:class,IHolonBase{
            var first=await codec.StageVersionAsync(row);
            var second=await codec.StageVersionAsync(row,first.Turtle);
            foreach(var version in new[]{0,1}){
                var loaded=await codec.DecodeVersionAsync<T>(second.Turtle,row.Id,version);
                Assert.Equal(precise,decimal.Parse(loaded.MetaData["precise"].ToString()!,System.Globalization.CultureInfo.InvariantCulture));
                Assert.IsType<string>(loaded.MetaData["literal_date"]);Assert.Equal(literalDate,loaded.MetaData["literal_date"]);
            }
        }
        if(source is Avatar avatar)await Check(avatar);else if(source is AvatarDetail detail)await Check(detail);else await Check((Holon)source);
    }
    [Fact]
    public void VersionedEnvelopeCannotRepairItsMissingEmbeddedIdentity()
    {
        using var codec=new SolidPrivateRecordCodec(Root,Key());IHolon row=new Holon{Id=Guid.NewGuid(),Version=1};
        var document=Newtonsoft.Json.Linq.JObject.Parse(NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(row));
        document[NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.EmbeddedRecordHistory.PropertyName]=new Newtonsoft.Json.Linq.JObject{["schema"]=1,["records"]=new Newtonsoft.Json.Linq.JArray()};
        var encode=typeof(SolidPrivateRecordCodec).GetMethod("EncodePayload",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.MakeGenericMethod(typeof(IHolon));
        var rdf=(string)encode.Invoke(codec,new object[]{row.Id,document.ToString()})!;
        Assert.Throws<System.IO.InvalidDataException>(()=>codec.Decode<IHolon>(rdf,row.Id));
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)]
    public async Task EncryptedSameResourceHistoryReusesCanonicalVersionChain(int kind)
    {
        using var codec=new SolidPrivateRecordCodec(Root,Key());
        IHolonBase source=kind==0?new Avatar{Id=Guid.NewGuid()}:kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon{Id=Guid.NewGuid()};
        source.Description="first-private-canary";
        async Task Check<T>(T original) where T:class,IHolonBase
        {
            var first=await codec.StageVersionAsync(original);Assert.Equal(0,original.Version);Assert.Equal(1,first.Record.Version);Assert.DoesNotContain(original.Description,first.Turtle);
            original.Description="second-private-canary";var second=await codec.StageVersionAsync(original,first.Turtle);
            Assert.Equal(2,second.Record.Version);Assert.Equal(first.Record.VersionId,second.Record.PreviousVersionId);Assert.NotEqual(first.Record.VersionId,second.Record.VersionId);
            Assert.Equal("first-private-canary",(await codec.DecodeVersionAsync<T>(second.Turtle,original.Id,1)).Description);
            Assert.Equal("second-private-canary",codec.Decode<T>(second.Turtle,original.Id).Description);
            Assert.Null(await codec.DecodeVersionAsync<T>(second.Turtle,original.Id,99));Assert.Equal(0,original.Version);
            Assert.DoesNotContain("first-private-canary",second.Turtle);Assert.DoesNotContain("second-private-canary",second.Turtle);
            using var reopened=new SolidPrivateRecordCodec(Root,Key());Assert.Equal(first.Record.VersionId,(await reopened.DecodeVersionAsync<T>(second.Turtle,original.Id,1)).VersionId);
        }
        if(source is IAvatar avatar)await Check(avatar);else if(source is IAvatarDetail detail)await Check(detail);else await Check((IHolon)source);
    }
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    [Fact]
    public void AvatarRoundTripUsesCanonicalFieldsWithoutPlaintextCredentials()
    {
        using var codec=new SolidPrivateRecordCodec(Root,Key());
        IAvatar avatar=new Avatar{Id=Guid.NewGuid(),Username="private-user-canary",Email="private-email@example.invalid",Password="private-password-canary",Version=7};
        var rdf=codec.Encode(avatar);Assert.DoesNotContain(avatar.Username,rdf);Assert.DoesNotContain(avatar.Email,rdf);Assert.DoesNotContain(avatar.Password,rdf);
        var restored=codec.Decode<IAvatar>(rdf,avatar.Id);
        Assert.Equal(avatar.Username,restored.Username);Assert.Equal(avatar.Email,restored.Email);Assert.Equal(avatar.Password,restored.Password);Assert.Equal(7,restored.Version);
        Assert.NotSame(avatar,restored);Assert.NotEqual(rdf,codec.Encode(avatar));
    }
    [Fact]
    public void AvatarDetailFieldsBeyondIdentityRoundTrip()
    {
        using var codec=new SolidPrivateRecordCodec(Root,Key());
        IAvatarDetail detail=new AvatarDetail{Id=Guid.NewGuid(),Username="detail-canary",Email="detail@example.invalid",Description="private detail",Version=3};
        var rdf=codec.Encode(detail);Assert.DoesNotContain(detail.Email,rdf);
        var restored=codec.Decode<IAvatarDetail>(rdf,detail.Id);
        Assert.Equal(detail.Username,restored.Username);Assert.Equal(detail.Email,restored.Email);Assert.Equal(detail.Description,restored.Description);Assert.Equal(3,restored.Version);
    }
    [Fact]
    public void HolonMetadataAndVersionRoundTrip()
    {
        using var codec=new SolidPrivateRecordCodec(Root,Key());
        IHolon holon=new Holon{Id=Guid.NewGuid(),Name="private oak",Description="quote \" and newline\n",Version=8,MetaData=new(){["canary"]="private metadata"}};
        var rdf=codec.Encode(holon);Assert.DoesNotContain("private metadata",rdf);
        var restored=codec.Decode<IHolon>(rdf,holon.Id);
        Assert.Equal(holon.Name,restored.Name);Assert.Equal(holon.Description,restored.Description);Assert.Equal("private metadata",restored.MetaData["canary"].ToString());Assert.Equal(8,restored.Version);
        using var wrongKey=new SolidPrivateRecordCodec(Root,new byte[32]);
        Assert.ThrowsAny<CryptographicException>(()=>wrongKey.Decode<IHolon>(rdf,holon.Id));
        Assert.ThrowsAny<Exception>(()=>codec.Decode<IHolon>(rdf,Guid.NewGuid()));
        Assert.ThrowsAny<Exception>(()=>codec.Decode<IAvatar>(rdf,holon.Id));
    }
    [Fact]
    public void PlainRdfCannotDowngradePrivateDecode()
    {
        using var codec=new SolidPrivateRecordCodec(Root,Key());
        Assert.ThrowsAny<Exception>(()=>codec.Decode<IHolon>("<https://pod.invalid/a> <https://oasisomniverse.one/ns/id> \"ignored\" .",Guid.NewGuid()));
    }
    [Fact]
    public void CiphertextMutationAndPodRelocationAreRejected()
    {
        using var codec=new SolidPrivateRecordCodec(Root,Key());IHolon record=new Holon{Id=Guid.NewGuid()};
        var rdf=codec.Encode(record);const string marker="oasis-aes256gcm-v1:";
        var offset=rdf.IndexOf(marker,StringComparison.Ordinal)+marker.Length;Assert.True(offset>=marker.Length);
        var mutated=rdf[..offset]+(rdf[offset]=='A'?'B':'A')+rdf[(offset+1)..];
        Assert.ThrowsAny<CryptographicException>(()=>codec.Decode<IHolon>(mutated,record.Id));
        using var otherPod=new SolidPrivateRecordCodec(new Uri("https://other-pod.invalid/private/"),Key());
        Assert.ThrowsAny<Exception>(()=>otherPod.Decode<IHolon>(rdf,record.Id));
    }

    [Fact]
    public void KeyIsCopiedAndDisposedCodecCannotEncode()
    {
        var key=Key();using var codec=new SolidPrivateRecordCodec(Root,key);Array.Clear(key);
        IHolon record=new Holon{Id=Guid.NewGuid()};var rdf=codec.Encode(record);
        using(var original=new SolidPrivateRecordCodec(Root,Key()))Assert.Equal(record.Id,original.Decode<IHolon>(rdf,record.Id).Id);
        codec.Dispose();Assert.Throws<ObjectDisposedException>(()=>codec.Encode(record));
    }
}
