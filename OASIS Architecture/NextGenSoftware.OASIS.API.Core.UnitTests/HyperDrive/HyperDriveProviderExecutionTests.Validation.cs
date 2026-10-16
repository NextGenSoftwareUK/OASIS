using FluentAssertions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    public static IEnumerable<object[]> InvalidCollectionQueries()
    {
        foreach(var operation in new[]{"LoadAllHolons","LoadHolonsForParent","LoadHolonsForParentByProviderKey","LoadHolonsByMetaData","LoadHolonsByMetaDataPairs"})
        foreach(var field in new[]{"type","depth","currentDepth"})
        foreach(var synchronous in new[]{false,true}) yield return new object[]{operation,field,synchronous};
    }

    [Theory]
    [MemberData(nameof(InvalidCollectionQueries))]
    public async Task InvalidCollectionQueryNeverSelectsOrCallsProvider(string operation,string field,bool synchronous)
    {
        var primary=CreateActiveProvider();var secondary=CreateActiveProvider(ProviderType.LocalFileOASIS,"secondary");
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoLoadBalanceEnabled=true,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(primary.Object);manager.RegisterProvider(secondary.Object);
        var types=new[]{new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(types);manager.SetAndReplaceAutoLoadBalanceListForProviders(types);manager.SetAndReplaceAutoReplicationListForProviders(types);
        var router=new OASISHyperDrive(manager);
        var request=new StorageOperationRequest {Operation=operation,HolonId=Guid.NewGuid(),ProviderKey=Guid.NewGuid().ToString(),MetaKey="owned",MetaValue="value",
            MetaKeyValuePairs=new Dictionary<string,string>{{"owned","value"}},HolonType=field=="type" ? (HolonType)int.MaxValue:HolonType.All,
            MaxChildDepth=field=="depth" ? -1:0,CurrentChildDepth=field=="currentDepth" ? -1:0};
        var result=synchronous ? router.RouteRequest<object>(request):await router.RouteRequestAsync<object>(request);
        result.IsError.Should().BeTrue();result.Result.Should().BeNull();
        result.ErrorCode.Should().Be(field=="type" ? "HYPERDRIVE_HOLON_TYPE_INVALID":"HYPERDRIVE_DEPTH_INVALID");
        result.Exception.Should().BeOfType<ArgumentOutOfRangeException>();
        manager.LastProviderSelectionDiagnostic.Should().BeNull();router.LastFailoverDiagnostic.Should().BeNull();
        foreach(var provider in new[]{primary,secondary}) provider.Invocations.Should().NotContain(call=>call.Method.Name.StartsWith("Load",StringComparison.Ordinal) || call.Method.Name.StartsWith("Save",StringComparison.Ordinal));
    }

    public static IEnumerable<object[]> InvalidStorageInputs()
    {
        var cases=new[] {
            ("SaveHolon","PAYLOAD_INVALID"),("LoadHolon","HOLON_ID_REQUIRED"),
            ("LoadHolonByProviderKey","PROVIDER_KEY_REQUIRED"),("LoadHolonsForParent","HOLON_ID_REQUIRED"),
            ("LoadHolonsByMetaData","META_KEY_REQUIRED"),("LoadHolonsByMetaDataPairs","METADATA_REQUIRED"),
            ("LoadAvatar","AVATAR_ID_REQUIRED"),("LoadAvatarByUsername","USERNAME_REQUIRED"),
            ("LoadAvatarByEmail","EMAIL_REQUIRED"),("LoadAvatarByProviderKey","PROVIDER_KEY_REQUIRED"),
            ("LoadAvatarByVerificationToken","VERIFICATION_TOKEN_REQUIRED"),("LoadAvatarByResetToken","RESET_TOKEN_REQUIRED"),
            ("LoadAvatarByRefreshToken","REFRESH_TOKEN_REQUIRED"),("LoadAvatarByPublicKey","PUBLIC_KEY_REQUIRED"),
            ("LoadAvatarByPrivateKey","PRIVATE_KEY_REQUIRED"),("Search","SEARCH_PARAMS_REQUIRED"),
            ("DeleteAvatarByProviderKey","PROVIDER_KEY_REQUIRED"),("DeleteAvatarDetail","AVATAR_ID_REQUIRED"),
            ("DeleteHolonSoft","SOFT_DELETE_REQUIRES_SAVE")};
        foreach(var item in cases) foreach(var synchronous in new[] {false,true})
            yield return new object[] {item.Item1,"HYPERDRIVE_"+item.Item2,synchronous};
    }

    [Theory]
    [MemberData(nameof(InvalidStorageInputs))]
    public async Task StorageInputErrorsNeverBecomeFailoverAttempts(string operation,string code,bool synchronous)
    {
        var primary=CreateActiveProvider();var stale=CreateActiveProvider(ProviderType.LocalFileOASIS,"stale-copy");
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        manager.RegisterProvider(primary.Object);manager.RegisterProvider(stale.Object);
        manager.SetAndReplaceAutoFailOverListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)}).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        var request=new StorageOperationRequest {Operation=operation=="DeleteHolonSoft" ? "DeleteHolon":operation,
            HolonId=operation=="DeleteHolonSoft" ? Guid.NewGuid():Guid.Empty,PreferredProvider=ProviderType.MongoDBOASIS};
        var result=synchronous ? router.RouteRequest<object>(request):await router.RouteRequestAsync<object>(request);
        result.IsError.Should().BeTrue();result.ErrorCode.Should().Be(code);router.LastFailoverDiagnostic.Should().BeNull();
        var explicitResult=await router.FailoverRequestAsync<object>(request);
        explicitResult.IsError.Should().BeTrue();explicitResult.ErrorCode.Should().Be(code);
        router.LastFailoverDiagnostic.Attempts.Should().ContainSingle();
        foreach(var provider in new[] {primary,stale})
            provider.Invocations.Should().NotContain(call=>call.Method.Name.StartsWith("Load",StringComparison.Ordinal)
                || call.Method.Name.StartsWith("Save",StringComparison.Ordinal) || call.Method.Name.StartsWith("Delete",StringComparison.Ordinal) || call.Method.Name=="Search",
                "validation happens before physical data I/O, including the explicit failover loop");
    }
}
