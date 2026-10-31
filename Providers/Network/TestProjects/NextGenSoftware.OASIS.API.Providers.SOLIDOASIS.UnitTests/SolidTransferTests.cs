using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidTransferTests
{
    [Theory][InlineData(0,false)][InlineData(0,true)][InlineData(1,false)][InlineData(1,true)][InlineData(2,false)][InlineData(2,true)][InlineData(3,false)][InlineData(3,true)]
    public async Task AllVersionExportsUseCurrentIdentityAndPerVersionOwner(int selection,bool sync)
    {
        var transport=new SolidRecordHttpTests.Transport();var owner=Guid.NewGuid();var other=Guid.NewGuid();var id=Guid.NewGuid();
        using var codec=new SolidPrivateRecordCodec(Root,Key());
        IAvatar avatar=new Avatar{Id=owner,Username="current-user",Email="current@example.invalid",Version=2};
        transport.Records[codec.Resource<IAvatar>(owner).AbsoluteUri]=codec.Encode(avatar);
        string? previous=null;
        foreach(var version in new[]{1,2,3,4}){
            IHolon row=new Holon{Id=id,Version=version,CreatedByAvatarId=version==2?other:owner,Name="private-canary-"+version,
                DeletedDate=version==4?new DateTime(2026,10,24,0,0,0,DateTimeKind.Utc):DateTime.MinValue};
            previous=(await codec.StageVersionAsync(row,previous)).Turtle;
        }
        transport.Records[codec.Resource<IHolon>(id).AbsoluteUri]=previous!;
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=selection switch {
            0=>sync?provider.ExportAll(-1):await provider.ExportAllAsync(-1),
            1=>sync?provider.ExportAllDataForAvatarById(owner,-1):await provider.ExportAllDataForAvatarByIdAsync(owner,-1),
            2=>sync?provider.ExportAllDataForAvatarByUsername("current-user",-1):await provider.ExportAllDataForAvatarByUsernameAsync("current-user",-1),
            _=>sync?provider.ExportAllDataForAvatarByEmail("current@example.invalid",-1):await provider.ExportAllDataForAvatarByEmailAsync("current@example.invalid",-1)};
        Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsLoaded);
        Assert.Equal(selection==0?new[]{1,2,3}:new[]{1,3},result.Result.Select(row=>row.Version));
        Assert.Equal(selection==0?3:2,result.LoadedCount);Assert.Equal(0,transport.Writes);
        Assert.Equal(previous,transport.Records[codec.Resource<IHolon>(id).AbsoluteUri]);
        var selected=await provider.ExportAllDataForAvatarByUsernameAsync("current-user",3);
        Assert.False(selected.IsError,selected.Exception?.ToString());Assert.Equal(3,Assert.Single(selected.Result).Version);
        var invalid=await provider.ExportAllAsync(-2);Assert.True(invalid.IsError);Assert.Null(invalid.Result);
        var ordinary=await provider.LoadAllHolonsAsync(version:-1);Assert.True(ordinary.IsError);Assert.Null(ordinary.Result);
    }
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    [Theory][InlineData(false)][InlineData(true)]
    public async Task ImportUsesEncryptedGraphWritesNotCustomEndpoint(bool synchronous)
    {
        var transport=new SolidRecordHttpTests.Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        IHolon child=new Holon{Name="private-canary-child"};IHolon root=new Holon{Name="private-canary-root",Children=new List<IHolon>{child}};
        var result=synchronous?provider.Import(new[]{root}):await provider.ImportAsync(new[]{root});
        Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.Result);Assert.Equal(2,transport.Writes);Assert.Equal(root.Id,child.ParentHolonId);
    }
    [Theory][InlineData(0,false)][InlineData(0,true)][InlineData(1,false)][InlineData(1,true)][InlineData(2,false)][InlineData(2,true)][InlineData(3,false)][InlineData(3,true)]
    public async Task ExportUsesEncryptedRecordsAndEstablishedCreatorScope(int selection,bool synchronous)
    {
        var transport=new SolidRecordHttpTests.Transport();var owner=Guid.NewGuid();var other=Guid.NewGuid();
        using var codec=new SolidPrivateRecordCodec(Root,Key());
        IAvatar avatar=new Avatar{Id=owner,Username="private-user",Email="private-email@example.invalid"};transport.Records[codec.Resource<IAvatar>(owner).AbsoluteUri]=codec.Encode(avatar);
        foreach(var creator in new[]{owner,owner,other}){IHolon row=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=creator,Name="private-canary"};transport.Records[codec.Resource<IHolon>(row.Id).AbsoluteUri]=codec.Encode(row);}
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=selection switch{
            0=>synchronous?provider.ExportAll():await provider.ExportAllAsync(),
            1=>synchronous?provider.ExportAllDataForAvatarById(owner):await provider.ExportAllDataForAvatarByIdAsync(owner),
            2=>synchronous?provider.ExportAllDataForAvatarByUsername("PRIVATE-USER"):await provider.ExportAllDataForAvatarByUsernameAsync("PRIVATE-USER"),
            _=>synchronous?provider.ExportAllDataForAvatarByEmail("PRIVATE-EMAIL@EXAMPLE.INVALID"):await provider.ExportAllDataForAvatarByEmailAsync("PRIVATE-EMAIL@EXAMPLE.INVALID")};
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(selection==0?3:2,result.Result.Count());Assert.Equal(result.Result.Count(),result.LoadedCount);
        if(selection!=0)Assert.All(result.Result,row=>Assert.Equal(owner,row.CreatedByAvatarId));
    }
    [Fact]
    public async Task PartialImportCannotReportSuccessOrDiscardMutationEvidence()
    {
        var transport=new SolidRecordHttpTests.Transport{RejectWrite=2};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        IHolon root=new Holon{Children=new List<IHolon>{new Holon()}};var result=await provider.ImportAsync(new[]{root});
        Assert.True(result.IsError);Assert.False(result.Result);Assert.Single(Assert.IsType<KeyValuePartialMutationException>(result.Exception).AcknowledgedKeys);
    }
}
