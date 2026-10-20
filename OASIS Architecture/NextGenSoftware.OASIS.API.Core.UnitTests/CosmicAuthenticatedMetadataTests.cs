using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    private sealed class MetadataResolver : IHolonMetadataKeyResolver
    {
        public bool Denied;
        public readonly List<HolonMetadataKeyRequest> Requests = new();
        public readonly List<HolonMetadataKeyMaterial> Leases = new();
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
        {
            Requests.Add(request);
            if (Denied) return new() { IsError = true, ErrorCode = "KEY_DENIED" };
            var lease = new HolonMetadataKeyMaterial(Enumerable.Repeat(request.KeyId == "key-a" ? (byte)17 : (byte)34,32).ToArray());
            Leases.Add(lease); return new(lease);
        }
    }
    private static HolonMetadataEncryptionPolicy MetadataPolicy(MetadataResolver resolver, string key="key-a", string context="root-scope")
        => new(context,key,resolver,new[]{"search-tag"});

    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task AuthenticatedSaveCapturesItsContextAndOwnsMixedChildPolicies(bool synchronous, bool perHolon)
    {
        var f = Fixture(true); var resolver = new MetadataResolver(); var policy = MetadataPolicy(resolver);
        f.Manager.MetadataEncryptionPolicy = policy;
        var childPolicy = MetadataPolicy(resolver,"key-b","child-scope");
        var root = new Holon { MetadataEncryptionOverride = perHolon ? policy : null,
            MetaData = new() { ["private-note"] = "root private", ["search-tag"] = "queryable" } };
        var child = new Holon { MetadataEncryptionOverride = childPolicy, MetaData = new() { ["private-note"] = "child private" } };
        root.Children.Add(child);
        OASISResult<IHolon> Acknowledge(IHolon staged)
        {
            staged.MetaData["__oasis_enc__"].ToString().Should().StartWith(HolonMetadataCipher.Prefix);
            staged.Children.Single().MetaData["__oasis_enc__"].ToString().Should().StartWith(HolonMetadataCipher.Prefix);
            staged.MetaData["search-tag"].Should().Be("queryable"); staged.MetaData.Should().NotContainKey("private-note");
            StoredRecordJson.Serialize<IHolon>(staged).Should().NotContain("MetadataEncryptionOverride").And.NotContain("Resolver");
            f.Manager.MetadataEncryptionPolicy = MetadataPolicy(new MetadataResolver(),context:"changed-during-io");
            return new(staged);
        }
        f.Provider.Setup(provider=>provider.SaveHolon(It.IsAny<IHolon>(),true,true,0,true,false))
            .Returns((IHolon value,bool a,bool b,int c,bool d,bool e)=>Acknowledge(value));
        f.Provider.Setup(provider=>provider.SaveHolonAsync(It.IsAny<IHolon>(),true,true,0,true,false))
            .ReturnsAsync((IHolon value,bool a,bool b,int c,bool d,bool e)=>Acknowledge(value));
        var result = synchronous ? f.Manager.SaveHolon(root,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS)
            : await f.Manager.SaveHolonAsync(root,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message+result.Exception); result.IsSaved.Should().BeTrue();
        root.MetaData["private-note"].ToString().Should().Be("root private"); child.MetaData["private-note"].ToString().Should().Be("child private");
        root.MetaData.Should().NotContainKey("__oasis_enc__"); child.MetaData.Should().NotContainKey("__oasis_enc__");
        root.MetadataEncryptionOverride.Should().BeSameAs(perHolon ? policy : null); child.MetadataEncryptionOverride.Should().BeSameAs(childPolicy);
        HolonChangeTracking.HasChanges(root).Should().BeFalse(); resolver.Leases.Should().OnlyContain(lease=>lease.IsDisposed);
        resolver.Requests.Should().Contain(request=>request.ContextId=="root-scope" && request.RecordId==root.Id)
            .And.Contain(request=>request.ContextId=="child-scope" && request.RecordId==child.Id);
    }

    [Theory]
    [InlineData(false,0)] [InlineData(true,0)] [InlineData(false,1)] [InlineData(true,1)]
    [InlineData(false,2)] [InlineData(true,2)]
    public async Task AuthenticatedSaveAdmissionFailsBeforeStorageAndPreservesCallerState(bool synchronous, int fault)
    {
        var f = Fixture(true); var resolver = new MetadataResolver { Denied = fault==0 }; var policy = MetadataPolicy(resolver);
        f.Manager.MetadataEncryptionPolicy = policy;
        var source = new Holon { MetaData = new() { ["private-note"] = "uncommitted" } };
        if (fault==1) source.MetaData["__oasis_enc__"]="unmaterialized";
        if (fault==2)
        {
            source.MetadataEncryptionOverride=policy;
            source.DataEncryptionOverride=new EncryptionSettings { Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('e',32) };
        }
        f.Provider.Invocations.Clear();
        var result = synchronous ? f.Manager.SaveHolon(source,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS)
            : await f.Manager.SaveHolonAsync(source,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeTrue(); result.IsSaved.Should().BeFalse();
        source.Id.Should().BeEmpty(); source.Version.Should().Be(0); source.Original.Should().BeNull();
        source.MetaData["private-note"].Should().Be("uncommitted");
        f.Provider.Invocations.Should().NotContain(invocation=>invocation.Method.Name.StartsWith("SaveHolon"));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void AuthenticatedReadUsesExplicitContextAndNeverTriesAnotherKeyOrFormat(int fault)
    {
        var f=Fixture(true); var writer=new MetadataResolver(); var id=Guid.NewGuid();
        var envelope=HolonMetadataCipher.Encrypt(id,"{\"private-note\":\"stored\"}",MetadataPolicy(writer));
        var reader=new MetadataResolver {Denied=fault==1};
        f.Manager.MetadataEncryptionPolicy=fault==0 ? null : MetadataPolicy(reader,"key-b",fault==2 ? "wrong-scope":"root-scope");
        var source=new Holon {Id=fault==3 ? Guid.NewGuid():id,MetaData=new(){{"__oasis_enc__",envelope}}};
        var result=f.Manager.DecryptHolonMetaData(source);
        result.IsError.Should().BeTrue(); source.MetaData["__oasis_enc__"].Should().Be(envelope);
        source.MetaData.Should().NotContainKey("private-note");
        result.ErrorCode.Should().Be(fault==0 ? "HOLON_METADATA_DECRYPTION_CONFIG_MISSING":fault==1 ? "KEY_DENIED":"HOLON_METADATA_DECRYPTION_FAILED");
        reader.Requests.Count.Should().Be(fault==1 ? 1:0);
    }

    [Fact]
    public void AuthenticatedReadAfterRestartUsesStoredKeyReferenceInsteadOfCurrentWriteKey()
    {
        var id=Guid.NewGuid(); var envelope=HolonMetadataCipher.Encrypt(id,"{\"private-note\":\"stored\"}",MetadataPolicy(new MetadataResolver()));
        var f=Fixture(true); var reader=new MetadataResolver(); f.Manager.MetadataEncryptionPolicy=MetadataPolicy(reader,"key-b");
        var source=new Holon {Id=id,MetaData=new(){{"__oasis_enc__",envelope}}};
        var result=f.Manager.DecryptHolonMetaData(source); result.IsError.Should().BeFalse(result.Message+result.Exception);
        source.MetaData["private-note"].ToString().Should().Be("stored"); reader.Requests.Single().KeyId.Should().Be("key-a");
    }
}
