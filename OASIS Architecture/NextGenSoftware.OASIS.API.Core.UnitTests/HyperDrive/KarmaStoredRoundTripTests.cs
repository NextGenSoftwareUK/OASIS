using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task KarmaLedgerSurvivesCanonicalStorageRoundTrips(bool synchronous)
    {
        // Controlled serialization-boundary test, NOT native SDK evidence. Every
        // read decodes new objects; no aliased in-memory entities hide codec bugs.
        var stored=new Dictionary<Guid,string>();var provider=CreateActiveProvider(ProviderType.AWSOASIS,"roundtrip-control");
        DeclareLeafChildReadContract(provider);
        OASISResult<IHolon> Read(Guid id)=>stored.TryGetValue(id,out var json)
            ?new OASISResult<IHolon>(StoredRecordJson.Deserialize<Holon>(json)) {IsLoaded=true}
            :new OASISResult<IHolon> {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound};
        OASISResult<IHolon> Save(IHolon row,bool children)
        {
            Assert.Equal(!stored.ContainsKey(row.Id),children);
            stored[row.Id]=StoredRecordJson.Serialize(row);
            return new OASISResult<IHolon>(StoredRecordJson.Deserialize<Holon>(stored[row.Id])) {IsSaved=true,SavedCount=1};
        }
        provider.Setup(value=>value.LoadHolon(It.IsAny<Guid>(),true,true,0,true,false,0))
            .Returns((Guid id,bool a,bool b,int c,bool d,bool e,int f)=>Read(id));
        provider.Setup(value=>value.LoadHolonAsync(It.IsAny<Guid>(),true,true,0,true,false,0))
            .ReturnsAsync((Guid id,bool a,bool b,int c,bool d,bool e,int f)=>Read(id));
        foreach(var children in new[] {false,true})
        {
            provider.Setup(value=>value.SaveHolon(It.IsAny<IHolon>(),children,true,0,true,false))
                .Returns((IHolon row,bool a,bool b,int c,bool d,bool e)=>Save(row,a));
            provider.Setup(value=>value.SaveHolonAsync(It.IsAny<IHolon>(),children,true,0,true,false))
                .ReturnsAsync((IHolon row,bool a,bool b,int c,bool d,bool e)=>Save(row,a));
        }
        var dna=CreateDna(HyperDriveModes.V2);var providers=new ProviderManager(null,dna)
            {IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        providers.RegisterProvider(provider.Object);
        var manager=new KarmaManager(provider.Object,dna,providers);var id=Guid.NewGuid();
        var result=synchronous?manager.AddKarma(id,500,KarmaSourceType.Game,"owned roundtrip")
            :await manager.AddKarmaAsync(id,500,KarmaSourceType.Game,"owned roundtrip");
        Assert.False(result.IsError,result.Message+" "+result.Exception);
        Assert.True(result.Result);
        var fresh=new KarmaManager(provider.Object,dna,providers);
        var balance=synchronous?fresh.GetKarma(id):await fresh.GetKarmaAsync(id);
        Assert.False(balance.IsError,balance.Message+" "+balance.Exception);
        Assert.Equal(500L,balance.Result);
    }
}
