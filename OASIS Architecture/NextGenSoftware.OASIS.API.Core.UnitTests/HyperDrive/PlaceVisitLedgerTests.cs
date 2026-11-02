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
    public async Task PlaceVisitAndKarmaPersistTogetherAndReplayDoesNotRewardAgain(bool failSave)
    {
        var stored=new Dictionary<Guid,string>();var provider=CreateActiveProvider(ProviderType.AWSOASIS,"place-visit-control");
        DeclareLeafChildReadContract(provider);
        OASISResult<IHolon> Read(Guid id)=>stored.TryGetValue(id,out var json)
            ?new OASISResult<IHolon>(StoredRecordJson.Deserialize<Holon>(json)){IsLoaded=true}
            :new OASISResult<IHolon>{IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound};
        OASISResult<IHolon> Save(IHolon row)
        {
            if(failSave)return new(){IsError=true,Message="Owned storage outage"};
            stored[row.Id]=StoredRecordJson.Serialize(row);
            return new(StoredRecordJson.Deserialize<Holon>(stored[row.Id])){IsSaved=true,SavedCount=1};
        }
        provider.Setup(p=>p.LoadHolon(It.IsAny<Guid>(),true,true,0,true,false,0)).Returns((Guid id,bool a,bool b,int c,bool d,bool e,int f)=>Read(id));
        provider.Setup(p=>p.LoadHolonAsync(It.IsAny<Guid>(),true,true,0,true,false,0)).ReturnsAsync((Guid id,bool a,bool b,int c,bool d,bool e,int f)=>Read(id));
        foreach(var children in new[]{false,true})
        {
            provider.Setup(p=>p.SaveHolon(It.IsAny<IHolon>(),children,true,0,true,false)).Returns((IHolon row,bool a,bool b,int c,bool d,bool e)=>Save(row));
            provider.Setup(p=>p.SaveHolonAsync(It.IsAny<IHolon>(),children,true,0,true,false)).ReturnsAsync((IHolon row,bool a,bool b,int c,bool d,bool e)=>Save(row));
        }
        var dna=CreateDna(HyperDriveModes.V2);var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};providers.RegisterProvider(provider.Object);
        var manager=new KarmaManager(provider.Object,dna,providers);var avatar=Guid.NewGuid();var firstId=Guid.NewGuid();
        var place=new PlaceVisitRecord{PlaceId="osm:node:123",Name="Owned Yoga Venue",Category="Yoga",Latitude=21,Longitude=-86};
        var first=await manager.ObservePlaceVisitAsync(avatar,place,true,firstId);
        if(failSave){Assert.True(first.IsError);Assert.Empty(stored);Assert.True((await manager.GetPlaceVisitsAsync(avatar)).IsError);return;}
        Assert.False(first.IsError,first.Message);Assert.Equal(100,first.Result.Karma);
        var restarted=new KarmaManager(provider.Object,dna,providers);
        var replay=await restarted.ObservePlaceVisitAsync(avatar,place,true,firstId);Assert.Equal(firstId,replay.Result.Id);
        Assert.Null((await restarted.ObservePlaceVisitAsync(avatar,place,true,Guid.NewGuid())).Result);
        var departure=Guid.NewGuid();Assert.False((await restarted.ObservePlaceVisitAsync(avatar,place,false,departure)).IsError);
        var again=await restarted.ObservePlaceVisitAsync(avatar,place,true,Guid.NewGuid());Assert.Equal(50,again.Result.Karma);Assert.False(again.Result.FirstVisit);
        await restarted.ObservePlaceVisitAsync(avatar,place,false,departure); // replaying an old departure cannot end a new visit.
        Assert.Single((await restarted.GetActivePlaceVisitsAsync(avatar)).Result);
        Assert.Equal(150,(await restarted.GetKarmaAsync(avatar)).Result);
        var history=(await restarted.GetPlaceVisitsAsync(avatar)).Result;Assert.Equal(2,history.Count);
        Assert.All(history,v=>{Assert.Equal("Our World",v.OApp);Assert.Equal(place.PlaceId,v.PlaceId);Assert.Equal(DateTimeKind.Utc,v.VisitedUtc.Kind);});
        var karma=(await restarted.GetKarmaHistoryAsync(avatar)).Result;Assert.Equal(2,karma.Count);Assert.All(karma,t=>Assert.Contains("Owned Yoga Venue",t.Description));
    }
}
