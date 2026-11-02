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
    public async Task GardenRewardCommitsOnceAndRepairsAuditAfterRestart(bool failSave)
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
        IAvatarDetail profile=new AvatarDetail{Id=avatar};
        provider.Setup(p=>p.LoadAvatarDetailAsync(avatar,0)).ReturnsAsync(()=>new OASISResult<IAvatarDetail>(profile));
        provider.Setup(p=>p.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>())).ReturnsAsync((IAvatarDetail row)=>{profile=row;return new OASISResult<IAvatarDetail>(row){IsSaved=true,SavedCount=1};});
        bool profileAvailable=false;
        provider.Setup(p=>p.LoadAvatarDetailAsync(avatar,0)).ReturnsAsync(()=>profileAvailable?new OASISResult<IAvatarDetail>(profile):new OASISResult<IAvatarDetail>{IsError=true,Message="Owned profile projection outage"});
        var plant=Guid.NewGuid();var earned=DateTime.UtcNow;
        var first=await manager.CreditGardenRewardAsync(avatar,firstId,plant,500,"Planted owned oak",earned);
        Assert.True(first.IsError);
        if(failSave){Assert.Empty(stored);return;}
        Assert.Equal(500,(await manager.GetKarmaAsync(avatar)).Result);
        profileAvailable=true;var restarted=new KarmaManager(provider.Object,dna,providers);
        var replays=await Task.WhenAll(Enumerable.Range(0,8).Select(_=>restarted.CreditGardenRewardAsync(avatar,firstId,plant,500,"Planted owned oak",earned)));
        Assert.All(replays,r=>{Assert.False(r.IsError,r.Message);Assert.Equal(500,r.Result);});
        Assert.Equal(500,profile.Karma);Assert.Single(profile.KarmaAkashicRecords);
        Assert.Contains("Planted owned oak",profile.KarmaAkashicRecords.Single().KarmaSourceDesc);
        Assert.Single((await restarted.GetKarmaHistoryAsync(avatar)).Result);
        Assert.True((await restarted.CreditGardenRewardAsync(avatar,firstId,plant,50,"Planted owned oak",earned)).IsError);
        Assert.Equal(500,(await restarted.GetKarmaAsync(avatar)).Result);
    }
}
