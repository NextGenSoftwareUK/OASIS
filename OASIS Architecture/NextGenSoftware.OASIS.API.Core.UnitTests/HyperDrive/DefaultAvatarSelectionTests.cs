using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public class DefaultAvatarSelectionTests
{
    public static IEnumerable<object[]> Cases()=>from kind in Enumerable.Range(0,5)
        from synchronous in new[]{false,true} from scenario in new[]{"duplicate","deleted","missing","upstream-error"}
        select new object[]{kind,synchronous,scenario};

    private static Avatar Record()=>new(){Id=Guid.NewGuid(),VerificationToken="owned-selector",ResetToken="owned-selector",
        RefreshTokens=new(){new RefreshToken{Token="owned-selector"}},ProviderWallets=new(){[ProviderType.EthereumOASIS]=new(){new ProviderWallet{PublicKey="owned-selector",PrivateKey="owned-selector"}}}};

    [Theory][MemberData(nameof(Cases))]
    public async Task DefaultCredentialSelectionNeverPublishesAmbiguousDeletedMissingOrFailedAvatar(int kind,bool synchronous,string scenario)
        => await Run(kind,synchronous,scenario,false);

    public static IEnumerable<object[]> BaseCases()=>from kind in Enumerable.Range(0,3)
        from synchronous in new[]{false,true} from scenario in new[]{"valid","duplicate","deleted","missing","upstream-error"}
        select new object[]{kind,synchronous,scenario};

    [Theory][MemberData(nameof(BaseCases))]
    public async Task InheritedTokenSelectionUsesRealCollectionBoundary(int kind,bool synchronous,string scenario)
        => await Run(kind,synchronous,scenario,true);

    private static async Task Run(int kind,bool synchronous,string scenario,bool inherited)
    {
        var avatar=Record();var rows=new List<IAvatar>{avatar};
        if(scenario=="duplicate")rows.Add(Record());
        if(scenario=="deleted")avatar.DeletedDate=DateTime.UtcNow;
        if(scenario=="missing")rows.Clear();
        var cause=new IOException("owned failed collection");
        var source=new OASISResult<IEnumerable<IAvatar>>(rows){IsLoaded=true,LoadedCount=rows.Count,
            IsError=scenario=="upstream-error",ErrorCode=scenario=="upstream-error"?"owned-upstream":"",
            Exception=scenario=="upstream-error"?cause:null,MetaData=new(){{"owned-diagnostic","retained"}}};
        var mock=new Mock<IOASISStorageProvider>{CallBase=true};
        mock.Setup(p=>p.LoadAllAvatarsAsync(0)).ReturnsAsync(source);mock.Setup(p=>p.LoadAllAvatars(0)).Returns(source);
        var baseMock=new Mock<OASISStorageProviderBase>{CallBase=true};
        baseMock.Setup(p=>p.LoadAllAvatarsAsync(0)).ReturnsAsync(source);baseMock.Setup(p=>p.LoadAllAvatars(0)).Returns(source);
        IOASISStorageProvider provider=inherited?baseMock.Object:mock.Object;
        var result=kind switch
        {
            0=>synchronous?provider.LoadAvatarByVerificationToken("owned-selector"):await provider.LoadAvatarByVerificationTokenAsync("owned-selector"),
            1=>synchronous?provider.LoadAvatarByResetToken("owned-selector"):await provider.LoadAvatarByResetTokenAsync("owned-selector"),
            2=>synchronous?provider.LoadAvatarByRefreshToken("owned-selector"):await provider.LoadAvatarByRefreshTokenAsync("owned-selector"),
            3=>synchronous?provider.LoadAvatarByPublicKey("owned-selector"):await provider.LoadAvatarByPublicKeyAsync("owned-selector"),
            _=>synchronous?provider.LoadAvatarByPrivateKey("owned-selector"):await provider.LoadAvatarByPrivateKeyAsync("owned-selector")
        };
        if(scenario=="valid")
        {Assert.False(result.IsError);Assert.Same(avatar,result.Result);Assert.True(result.IsLoaded);Assert.Equal(1,result.LoadedCount);}
        else
        {
            Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);
            Assert.Equal("retained",result.MetaData["owned-diagnostic"]);
            Assert.Equal(scenario=="duplicate"?OASISResultErrorCodes.AmbiguousIdentity:scenario=="upstream-error"?"owned-upstream":OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
            if(scenario=="upstream-error")Assert.Same(cause,result.Exception);
        }
        if(inherited)
        {baseMock.Verify(p=>p.LoadAllAvatars(0),synchronous?Times.Once():Times.Never());baseMock.Verify(p=>p.LoadAllAvatarsAsync(0),synchronous?Times.Never():Times.Once());}
        else
        {mock.Verify(p=>p.LoadAllAvatars(0),synchronous?Times.Once():Times.Never());mock.Verify(p=>p.LoadAllAvatarsAsync(0),synchronous?Times.Never():Times.Once());}
    }
}
