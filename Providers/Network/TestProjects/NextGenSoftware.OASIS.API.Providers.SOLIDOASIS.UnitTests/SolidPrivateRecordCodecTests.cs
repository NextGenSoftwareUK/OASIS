using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidPrivateRecordCodecTests
{
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
