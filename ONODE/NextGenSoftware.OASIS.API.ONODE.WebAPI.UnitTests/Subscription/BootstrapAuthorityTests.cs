using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using NextGenSoftware.OASIS.API.Core.Enums;

public class BootstrapAuthorityTests
{
    private static readonly Guid Avatar = Guid.Parse("42282c29-55b7-4196-b1ac-095052227684");
    private static readonly DateTime Now = new(2026,10,18,2,0,0,DateTimeKind.Utc);
    private static BootstrapAuthorityRecord Valid() => new()
    { Id=Avatar.ToString("D"),Revision=2,Ready=true,Enabled=true,Role=AvatarType.User,TokenVersion=3,KarmaVersion=4,
        Karma=123,VerifiedUntilUtc=Now.AddMinutes(1) };

    [Fact] public void ExactFreshReadyIdentityIsAccepted() => MongoBootstrapAuthority.Validate(Valid(),Avatar,3,Now);
    [Fact] public void MissingIdentityFailsClosed() => Assert.Throws<InvalidOperationException>(()=>MongoBootstrapAuthority.Validate(null!,Avatar,3,Now));
    [Fact] public void WrongIdentityFailsClosed()
    { var r=Valid();r.Id=Guid.NewGuid().ToString("D");Assert.Throws<InvalidOperationException>(()=>MongoBootstrapAuthority.Validate(r,Avatar,3,Now)); }
    [Fact] public void UnfinishedMutationFailsClosed()
    { var r=Valid();r.Ready=false;Assert.Throws<InvalidOperationException>(()=>MongoBootstrapAuthority.Validate(r,Avatar,3,Now)); }
    [Fact] public void DisabledIdentityIsRejected()
    { var r=Valid();r.Enabled=false;Assert.Throws<UnauthorizedAccessException>(()=>MongoBootstrapAuthority.Validate(r,Avatar,3,Now)); }
    [Theory][InlineData(2)][InlineData(4)] public void OldAndFutureTokenVersionsAreRejected(long version)
        => Assert.Throws<UnauthorizedAccessException>(()=>MongoBootstrapAuthority.Validate(Valid(),Avatar,version,Now));
    [Theory][InlineData(0)][InlineData(-1)] public void ExpiredIncludingExactBoundaryFailsClosed(int seconds)
    { var r=Valid();r.VerifiedUntilUtc=Now.AddSeconds(seconds);Assert.Throws<InvalidOperationException>(()=>MongoBootstrapAuthority.Validate(r,Avatar,3,Now)); }
    [Fact] public void MissingKarmaVersionFailsClosed()
    { var r=Valid();r.KarmaVersion=0;Assert.Throws<InvalidOperationException>(()=>MongoBootstrapAuthority.Validate(r,Avatar,3,Now)); }
    [Fact] public void LocalClockIsNotSilentlyConverted()
        => Assert.Throws<ArgumentException>(()=>MongoBootstrapAuthority.Validate(Valid(),Avatar,3,DateTime.SpecifyKind(Now,DateTimeKind.Unspecified)));
    [Theory][InlineData(null)][InlineData(999)] public void MissingOrUnknownRoleIsNotDefaultWizard(int? role)
    { var r=Valid();r.Role=role.HasValue?(AvatarType?)role.Value:null;Assert.Throws<InvalidOperationException>(()=>MongoBootstrapAuthority.Validate(r,Avatar,3,Now)); }
}
