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
    [InlineData(false)] [InlineData(true)]
    public void V2KarmaRecordProviderUsesBoundManagerRatherThanGlobalCurrent(bool bound)
    {
        var initial=CreateActiveProvider();var requested=CreateActiveProvider(ProviderType.AWSOASIS,"bound-aws");
        var dna=CreateDna(HyperDriveModes.V2);var providers=new ProviderManager(null,dna);
        providers.RegisterProvider(initial.Object);providers.RegisterProvider(requested.Object);
        providers.SetAndActivateCurrentStorageProvider(initial.Object);
        var manager=new KarmaManager(bound?requested.Object:null,dna,providers);
        var factory=typeof(KarmaManager).GetMethod("CreateKarmaRecord",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        var record=(Core.Objects.KarmaAkashicRecord)factory.Invoke(manager,new object?[]
            {Guid.NewGuid(),10,KarmaSourceType.Other,"owned","affinity",null,KarmaTypePositive.Walking100Steps,KarmaTypeNegative.None,KarmaEarntOrLost.Earnt})!;
        Assert.Equal(bound?ProviderType.AWSOASIS:ProviderType.MongoDBOASIS,record.Provider.Value);
        Assert.Same(initial.Object,providers.CurrentStorageProvider);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task V2KarmaProviderRegistrationFailureMustNotPoisonManagerCache(bool synchronous)
    {
        var initial=CreateActiveProvider();var requested=CreateActiveProvider(ProviderType.AWSOASIS,"late-aws");
        var dna=CreateDna(HyperDriveModes.V2);
        var providers=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        providers.RegisterProvider(initial.Object);providers.SetAndActivateCurrentStorageProvider(initial.Object);
        var id=Guid.NewGuid();
        requested.Setup(value=>value.LoadAvatarDetail(id,0)).Returns(new OASISResult<IAvatarDetail>
            {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="late registration read boundary"});
        var manager=new AvatarManager(initial.Object,dna,providers);
        async Task Invoke()
        {
            if(synchronous)Assert.True(manager.AddKarmaToAvatar(id,KarmaTypePositive.Other,KarmaSourceType.Other,"owned","late registration",providerType:ProviderType.AWSOASIS).IsError);
            else Assert.Null(await manager.AddKarmaToAvatarAsync(id,KarmaTypePositive.Other,KarmaSourceType.Other,"owned","late registration",providerType:ProviderType.AWSOASIS));
        }
        await Invoke();
        Assert.DoesNotContain(initial.Invocations,call=>call.Method.Name is "LoadAvatarDetail" or "LoadAvatarDetailAsync");
        providers.RegisterProvider(requested.Object);
        await Invoke();
        requested.Verify(value=>value.LoadAvatarDetail(id,0),Times.Once);
        Assert.DoesNotContain(initial.Invocations,call=>call.Method.Name is "LoadAvatarDetail" or "LoadAvatarDetailAsync");
        Assert.Same(initial.Object,providers.CurrentStorageProvider);
    }

    [Theory]
    [InlineData(false,false,false,false)] [InlineData(false,false,false,true)]
    [InlineData(false,false,true,false)] [InlineData(false,false,true,true)]
    [InlineData(false,true,false,false)] [InlineData(false,true,false,true)]
    [InlineData(false,true,true,false)] [InlineData(false,true,true,true)]
    [InlineData(true,false,false,false)] [InlineData(true,false,false,true)]
    [InlineData(true,false,true,false)] [InlineData(true,false,true,true)]
    [InlineData(true,true,false,false)] [InlineData(true,true,false,true)]
    [InlineData(true,true,true,false)] [InlineData(true,true,true,true)]
    public async Task V2KarmaExplicitProviderMustOwnInitialRead(bool synchronous,bool remove,bool payload,bool explicitProvider)
    {
        var initial=CreateActiveProvider();
        var requested=CreateActiveProvider(ProviderType.AWSOASIS,"requested-aws");
        var dna=CreateDna(HyperDriveModes.V2);
        var providers=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        providers.RegisterProvider(initial.Object);providers.RegisterProvider(requested.Object);
        providers.SetAndActivateCurrentStorageProvider(initial.Object);
        var id=Guid.NewGuid();
        foreach(var provider in new[] {initial,requested})
        {
            // A semantic absence prevents all ledger/projection writes. Neither mock
            // supplies a successful read or claims a real native implementation.
            provider.Setup(value=>value.LoadAvatarDetail(id,0)).Returns(new OASISResult<IAvatarDetail>
                {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="owned affinity read boundary"});
            provider.Setup(value=>value.LoadAvatarDetailAsync(id,0)).ReturnsAsync(new OASISResult<IAvatarDetail>
                {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="owned affinity read boundary"});
        }
        var manager=new AvatarManager(initial.Object,dna,providers);
        var avatar=new AvatarDetail {Id=id};
        var target=explicitProvider?ProviderType.AWSOASIS:ProviderType.Default;
        if(synchronous)
        {
            if(remove)
            {
                if(payload)_=manager.RemoveKarmaFromAvatar(avatar,KarmaTypeNegative.Other,KarmaSourceType.Other,"owned","affinity",providerType:target);
                else _=manager.RemoveKarmaFromAvatar(id,KarmaTypeNegative.Other,KarmaSourceType.Other,"owned","affinity",providerType:target);
            }
            else _=payload?manager.AddKarmaToAvatar(avatar,KarmaTypePositive.Other,KarmaSourceType.Other,"owned","affinity",providerType:target)
                :manager.AddKarmaToAvatar(id,KarmaTypePositive.Other,KarmaSourceType.Other,"owned","affinity",providerType:target);
        }
        else
        {
            _=remove
                ?payload?await manager.RemoveKarmaFromAvatarAsync(avatar,KarmaTypeNegative.Other,KarmaSourceType.Other,"owned","affinity",providerType:target)
                    :await manager.RemoveKarmaFromAvatarAsync(id,KarmaTypeNegative.Other,KarmaSourceType.Other,"owned","affinity",providerType:target)
                :payload?await manager.AddKarmaToAvatarAsync(avatar,KarmaTypePositive.Other,KarmaSourceType.Other,"owned","affinity",providerType:target)
                    :await manager.AddKarmaToAvatarAsync(id,KarmaTypePositive.Other,KarmaSourceType.Other,"owned","affinity",providerType:target);
        }
        static int Reads(Mock<IOASISStorageProvider> provider)=>provider.Invocations.Count(call=>call.Method.Name is "LoadAvatarDetail" or "LoadAvatarDetailAsync");
        Assert.Equal(explicitProvider?0:1,Reads(initial));
        Assert.Equal(explicitProvider?1:0,Reads(requested));
        foreach(var provider in new[] {initial,requested})
            Assert.DoesNotContain(provider.Invocations,call=>call.Method.Name is "SaveHolon" or "SaveHolonAsync" or "SaveAvatarDetail" or "SaveAvatarDetailAsync");
        Assert.Same(initial.Object,providers.CurrentStorageProvider);
    }
}
