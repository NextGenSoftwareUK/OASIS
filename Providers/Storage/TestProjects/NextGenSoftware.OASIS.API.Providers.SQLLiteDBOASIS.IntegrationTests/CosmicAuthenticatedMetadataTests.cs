using FluentAssertions;
using System.Text.Json.Nodes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public sealed class NativeMetadataResolver : IHolonMetadataKeyResolver
    {
        public bool Denied;
        public List<HolonMetadataKeyRequest> Requests { get; } = new();
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
        {
            Requests.Add(request);
            if (Denied || request.KeyId is not ("key-a" or "key-b")) return new() { IsError=true,ErrorCode="KEY_DENIED" };
            return new(new HolonMetadataKeyMaterial(Enumerable.Repeat(request.KeyId=="key-a" ? (byte)17:(byte)34,32).ToArray()));
        }
    }
    private static HolonMetadataEncryptionPolicy NativeMetadataPolicy(NativeMetadataResolver resolver,string key="key-a",string context="native-scope")
        => new(context,key,resolver,new[]{"search-tag"});

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AuthenticatedMetadataSurvivesFreshManagerGraphAndCqlReadsAndWriteKeyRotation(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var writer=CosmicManager(provider,true); var writerKeys=new NativeMetadataResolver();
        writer.MetadataEncryptionPolicy=NativeMetadataPolicy(writerKeys);
        var child=new Holon {MetadataEncryptionOverride=NativeMetadataPolicy(writerKeys,"key-b"),MetaData=new(){{"private-note","child private"}}};
        const decimal price=1.000000000000000000000000001m;
        var source=new NativeDecimalRow {Price=price,Children=new List<IHolon>{child},MetaData=new(){{"private-note","root private"},{"search-tag","coffee"}}};
        var saved=synchronous ? writer.SaveHolon(source,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS)
            :await writer.SaveHolonAsync(source,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        var stored=(await provider.LoadHolonAsync(source.Id,loadChildren:false)).Result;
        stored.MetaData["__oasis_enc__"].ToString().Should().StartWith("oasis2:").And.NotContain("root private");
        stored.MetaData["search-tag"].ToString().Should().Be("coffee");
        StoredRecordJson.Serialize(stored).Should().NotContain("MetadataEncryptionOverride").And.NotContain("Resolver");

        using var freshProvider=new SQLLiteDBOASIS(TestConnectionString);
        (await freshProvider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var reader=CosmicManager(freshProvider,true); var readerKeys=new NativeMetadataResolver();
        reader.MetadataEncryptionPolicy=NativeMetadataPolicy(readerKeys,"key-b");
        var read=synchronous ? reader.LoadHolon<NativeDecimalRow>(source.Id,maxChildDepth:1,providerType:ProviderType.SQLLiteDBOASIS)
            :await reader.LoadHolonAsync<NativeDecimalRow>(source.Id,maxChildDepth:1,providerType:ProviderType.SQLLiteDBOASIS);
        read.IsError.Should().BeFalse(read.Message+read.Exception); read.Result.Price.Should().Be(price);
        read.Result.Children.Single().MetaData["private-note"].ToString().Should().Be("child private");
        HolonChangeTracking.HasChanges(read.Result).Should().BeFalse();
        readerKeys.Requests.Should().Contain(request=>request.RecordId==source.Id && request.KeyId=="key-a")
            .And.Contain(request=>request.RecordId==child.Id && request.KeyId=="key-b");
        ICosmicQueryStorageProvider adapter=freshProvider; var id=source.Id;
        var query=CosmicQuery<NativeDecimalRow>.Create().Where(row=>row.Id==id).Result;
        var plan=CosmicQueryPlanner.Plan(query,adapter.QueryProviderType,new[]{new CosmicQueryTarget(adapter.QueryProviderType,adapter.QueryCapabilities)});
        plan.IsError.Should().BeFalse(plan.Message);
        var queried=await reader.ExecuteQueryAsync(plan.Result,new[]{adapter}); queried.IsError.Should().BeFalse(queried.Message+queried.Exception);
        var row=queried.Result.Providers.Single().Rows.Single(); row.Price.Should().Be(price); row.Price=1.000000000000000000000000002m;
        var edited=await reader.SaveHolonAsync(row,Guid.NewGuid(),saveChildren:false,providerType:ProviderType.SQLLiteDBOASIS);
        edited.IsError.Should().BeFalse(edited.Message+edited.Exception);
        var rotated=(await freshProvider.LoadHolonAsync(source.Id,loadChildren:false)).Result;
        var envelope=JsonNode.Parse(rotated.MetaData["__oasis_enc__"].ToString()!.Substring("oasis2:".Length))!;
        envelope["keyId"]!.GetValue<string>().Should().Be("key-b"); rotated.Version.Should().Be(stored.Version+1);
        var final=await reader.LoadHolonAsync<NativeDecimalRow>(source.Id,loadChildren:false,providerType:ProviderType.SQLLiteDBOASIS);
        final.IsError.Should().BeFalse(final.Message+final.Exception); final.Result.Price.Should().Be(row.Price);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public async Task AuthenticatedStoredReadRejectsUnauthorizedOrTamperedContextWithoutChangingStorage(int fault)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var writer=CosmicManager(provider,true); writer.MetadataEncryptionPolicy=NativeMetadataPolicy(new NativeMetadataResolver());
        var source=new Holon {MetaData=new(){{"private-note","private"}}};
        var saved=await writer.SaveHolonAsync(source,Guid.NewGuid(),saveChildren:false,providerType:ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        var target=(await provider.LoadHolonAsync(source.Id,loadChildren:false)).Result;
        if(fault==2 || fault==3)
        {
            var cipher=target.MetaData["__oasis_enc__"].ToString()!;
            if(fault==2) target=new Holon {Id=Guid.NewGuid(),MetaData=new(){{"__oasis_enc__",cipher}}};
            else
            {
                var envelope=JsonNode.Parse(cipher.Substring("oasis2:".Length))!;
                var bytes=Convert.FromBase64String(envelope["tag"]!.GetValue<string>()); bytes[0]^=1;
                envelope["tag"]=Convert.ToBase64String(bytes); target.MetaData["__oasis_enc__"]="oasis2:"+envelope.ToJsonString();
            }
            (await provider.SaveHolonAsync(target,saveChildren:false)).IsError.Should().BeFalse();
        }
        var before=StoredRecordJson.Serialize((await provider.LoadHolonAsync(target.Id,loadChildren:false)).Result);
        var reader=CosmicManager(provider,true); var resolver=new NativeMetadataResolver {Denied=fault==0};
        reader.MetadataEncryptionPolicy=fault==4 ? null:NativeMetadataPolicy(resolver,context:fault==1 ? "wrong-scope":"native-scope");
        var loaded=await reader.LoadHolonAsync(target.Id,loadChildren:false,providerType:ProviderType.SQLLiteDBOASIS);
        loaded.IsError.Should().BeTrue(); loaded.IsLoaded.Should().BeFalse(); loaded.Result.Should().BeNull();
        loaded.ErrorCode.Should().Be(fault==0 ? "KEY_DENIED":fault==4 ? "HOLON_METADATA_DECRYPTION_CONFIG_MISSING":"HOLON_METADATA_DECRYPTION_FAILED");
        resolver.Requests.Count.Should().Be(fault==0 || fault==3 ? 1:0);
        StoredRecordJson.Serialize((await provider.LoadHolonAsync(target.Id,loadChildren:false)).Result).Should().Be(before);
    }
}
