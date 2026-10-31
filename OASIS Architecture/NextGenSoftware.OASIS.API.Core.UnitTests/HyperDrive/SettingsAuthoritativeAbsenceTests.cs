using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    private static OASISResult<IHolon> ColdSettingsReadFailure(int kind)=>new()
    {
        IsError=true,ErrorCount=1,Message="owned settings read boundary",
        ErrorCode=kind==0?OASISResultErrorCodes.RecordNotFound:kind==1?"OWNED_TRANSPORT_FAILURE":OASISResultErrorCodes.AmbiguousIdentity,
        Exception=kind==1?new IOException("owned transport failure"):null
    };

    [Theory]
    [InlineData(false,0)] [InlineData(true,0)]
    [InlineData(false,1)] [InlineData(true,1)]
    [InlineData(false,2)] [InlineData(true,2)]
    public async Task V2SettingsColdStartCreatesOnlyAfterAuthoritativeAbsence(bool synchronous,int kind)
    {
        var provider=CreateActiveProvider();var dna=CreateDna(HyperDriveModes.V2);
        var providers=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        providers.RegisterProvider(provider.Object);
        provider.Setup(value=>value.LoadHolon(It.IsAny<Guid>(),true,true,0,true,false,0)).Returns(()=>ColdSettingsReadFailure(kind));
        provider.Setup(value=>value.LoadHolonAsync(It.IsAny<Guid>(),true,true,0,true,false,0)).ReturnsAsync(()=>ColdSettingsReadFailure(kind));
        provider.Setup(value=>value.SaveHolon(It.IsAny<IHolon>(),true,true,0,true,false))
            .Returns((IHolon row,bool a,bool b,int c,bool d,bool e)=>new OASISResult<IHolon>(row) {IsSaved=true,SavedCount=1});
        provider.Setup(value=>value.SaveHolonAsync(It.IsAny<IHolon>(),true,true,0,true,false))
            .ReturnsAsync((IHolon row,bool a,bool b,int c,bool d,bool e)=>new OASISResult<IHolon>(row) {IsSaved=true,SavedCount=1});
        var manager=new HolonManager(provider.Object,dna,providers);
        var result=synchronous?manager.GetAllSettings(Guid.NewGuid(),"karma"):await manager.GetAllSettingsAsync(Guid.NewGuid(),"karma");
        Assert.Equal(kind!=0,result.IsError);
        var writes=provider.Invocations.Count(call=>call.Method.Name is "SaveHolon" or "SaveHolonAsync");
        Assert.Equal(kind==0?1:0,writes);
        if(kind==0)Assert.NotNull(result.Result);
    }

    [Theory]
    [InlineData(false,0)] [InlineData(true,0)]
    [InlineData(false,1)] [InlineData(true,1)]
    [InlineData(false,2)] [InlineData(true,2)]
    public async Task V2MissingHolonAllowanceDoesNotHideNonAbsenceFailures(bool allowMissing,int kind)
    {
        var provider=CreateActiveProvider();var dna=CreateDna(HyperDriveModes.V2);
        var providers=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        providers.RegisterProvider(provider.Object);
        provider.Setup(value=>value.LoadHolonAsync(It.IsAny<Guid>(),true,true,0,true,false,0)).ReturnsAsync(()=>ColdSettingsReadFailure(kind));
        var manager=new HolonManager(provider.Object,dna,providers);
        var result=await manager.LoadHolonAsync(Guid.NewGuid(),allowMissing:allowMissing);
        // V2 ConvertResult already normalizes authoritative absence for both
        // ordinary and allowMissing reads; only non-absence failures remain errors.
        Assert.Equal(kind!=0,result.IsError);
        Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);
        if(kind==0){Assert.Equal(0,result.ErrorCount);Assert.Null(result.Exception);Assert.Equal(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);}
        Assert.DoesNotContain(provider.Invocations,call=>call.Method.Name is "SaveHolon" or "SaveHolonAsync");
    }
}
