using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(false,true)]
    [InlineData(true,false)] [InlineData(true,true)]
    public async Task V2AccountVersionOverflowRejectsBeforeMutationOrDispatch(bool detail,bool synchronous)
    {
        var provider=CreateActiveProvider();var dna=CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security=new SecuritySettings();
        var providers=new ProviderManager(null,dna)
            {IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        providers.RegisterProvider(provider.Object);providers.SetAndActivateCurrentStorageProvider(provider.Object);
        var manager=new AvatarManager(provider.Object,dna,providers);
        var id=Guid.NewGuid();var revision=Guid.NewGuid();var predecessor=Guid.NewGuid();
        var created=DateTime.UtcNow.AddDays(-1);
        var account=new Avatar {Id=id,Version=int.MaxValue,VersionId=revision,PreviousVersionId=predecessor,
            CreatedDate=created,Password="$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy"};
        var profile=new AvatarDetail {Id=id,Version=int.MaxValue,VersionId=revision,PreviousVersionId=predecessor,CreatedDate=created};
        if(detail)
        {
            var result=synchronous?manager.SaveAvatarDetail(profile):await manager.SaveAvatarDetailAsync(profile);
            Assert.True(result.IsError);Assert.Equal("AVATAR_VERSION_OVERFLOW",result.ErrorCode);
            Assert.IsType<OverflowException>(result.Exception);Assert.False(result.IsSaved);
        }
        else
        {
            var result=synchronous?manager.SaveAvatar(account):await manager.SaveAvatarAsync(account);
            Assert.True(result.IsError);Assert.Equal("AVATAR_VERSION_OVERFLOW",result.ErrorCode);
            Assert.IsType<OverflowException>(result.Exception);Assert.False(result.IsSaved);
        }
        foreach(IHolonBase value in new IHolonBase[]{account,profile})
        {
            Assert.Equal(int.MaxValue,value.Version);Assert.Equal(revision,value.VersionId);
            Assert.Equal(predecessor,value.PreviousVersionId);Assert.Equal(created,value.CreatedDate);
            Assert.Equal(DateTime.MinValue,value.ModifiedDate);
        }
        Assert.DoesNotContain(provider.Invocations,call=>call.Method.Name is "SaveAvatar" or "SaveAvatarAsync" or "SaveAvatarDetail" or "SaveAvatarDetailAsync");
    }

    [Theory]
    [InlineData(false,false,false)] [InlineData(false,true,false)]
    [InlineData(true,false,false)] [InlineData(true,true,false)]
    [InlineData(false,false,true)] [InlineData(false,true,true)]
    [InlineData(true,false,true)] [InlineData(true,true,true)]
    public async Task V2AccountSaveAdvancesLogicalVersionBeforeProviderDispatch(bool detail,bool synchronous,bool fresh)
    {
        var provider=CreateActiveProvider();
        var dna=CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security=new SecuritySettings();
        var providers=new ProviderManager(null,dna)
            {IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        providers.RegisterProvider(provider.Object);
        providers.SetAndActivateCurrentStorageProvider(provider.Object);
        var manager=new AvatarManager(provider.Object,dna,providers);
        var id=Guid.NewGuid();var previous=fresh?Guid.Empty:Guid.NewGuid();
        var seen=new List<(int Version,Guid VersionId,Guid PreviousVersionId)>();
        IAvatar CaptureAvatar(IAvatar value)
        {seen.Add((value.Version,value.VersionId,value.PreviousVersionId));return value;}
        IAvatarDetail CaptureDetail(IAvatarDetail value)
        {seen.Add((value.Version,value.VersionId,value.PreviousVersionId));return value;}
        provider.Setup(value=>value.SaveAvatarAsync(It.IsAny<IAvatar>()))
            .Returns((IAvatar value)=>Task.FromResult(new OASISResult<IAvatar>(CaptureAvatar(value)){IsSaved=true}));
        provider.Setup(value=>value.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>()))
            .Returns((IAvatarDetail value)=>Task.FromResult(new OASISResult<IAvatarDetail>(CaptureDetail(value)){IsSaved=true}));
        provider.Setup(value=>value.SaveAvatar(It.IsAny<IAvatar>()))
            .Returns((IAvatar value)=>new OASISResult<IAvatar>(CaptureAvatar(value)){IsSaved=true});
        provider.Setup(value=>value.SaveAvatarDetail(It.IsAny<IAvatarDetail>()))
            .Returns((IAvatarDetail value)=>new OASISResult<IAvatarDetail>(CaptureDetail(value)){IsSaved=true});
        var avatar=new Avatar {Id=id,Username="owned-version-fixture",Email="owned-version@example.invalid",
            Password="$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy",
            Version=fresh?0:7,VersionId=previous,CreatedDate=fresh?DateTime.MinValue:DateTime.UtcNow.AddDays(-1),IsNewHolon=fresh};
        var avatarDetail=new AvatarDetail {Id=id,Version=fresh?0:7,VersionId=previous,
            CreatedDate=fresh?DateTime.MinValue:DateTime.UtcNow.AddDays(-1),IsNewHolon=fresh};
        for(var iteration=0;iteration<2;iteration++)
        {
            if(detail)
            {
                var result=synchronous?manager.SaveAvatarDetail(avatarDetail):await manager.SaveAvatarDetailAsync(avatarDetail);
                Assert.False(result.IsError,result.Message);Assert.True(result.IsSaved);
                avatarDetail=Assert.IsAssignableFrom<AvatarDetail>(result.Result);
            }
            else
            {
                var result=synchronous?manager.SaveAvatar(avatar):await manager.SaveAvatarAsync(avatar);
                Assert.False(result.IsError,result.Message);Assert.True(result.IsSaved);
                avatar=Assert.IsAssignableFrom<Avatar>(result.Result);
            }
        }
        Assert.Equal(2,seen.Count);
        Assert.Equal(fresh?1:8,seen[0].Version);Assert.Equal(previous,seen[0].PreviousVersionId);
        Assert.NotEqual(Guid.Empty,seen[0].VersionId);Assert.NotEqual(previous,seen[0].VersionId);
        Assert.Equal(fresh?2:9,seen[1].Version);Assert.Equal(seen[0].VersionId,seen[1].PreviousVersionId);
        Assert.NotEqual(Guid.Empty,seen[1].VersionId);Assert.NotEqual(seen[0].VersionId,seen[1].VersionId);
    }
}
