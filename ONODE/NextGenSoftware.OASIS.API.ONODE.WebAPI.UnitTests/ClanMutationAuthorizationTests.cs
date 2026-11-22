using System.Net;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class ClanMutationAuthorizationTests
{
    [Theory]
    [InlineData("PROVIDER_UNAVAILABLE", HttpStatusCode.ServiceUnavailable)]
    [InlineData(OASISResultErrorCodes.RecordNotFound, HttpStatusCode.NotFound)]
    public void FailedOwnershipReadsRefuseMutationEvenForWizard(string code, HttpStatusCode status)
    {
        var lookup = new OASISResult<IClan> { IsError = true, ErrorCode = code };

        var refusal = ClanController.GetClanMutationRefusal(lookup, Guid.NewGuid(), isWizard: true);

        Assert.NotNull(refusal);
        Assert.Equal(status, refusal.StatusCode);
        Assert.True(refusal.Result.IsError);
        Assert.Equal(code, refusal.Result.ErrorCode);
    }

    [Fact]
    public void ClanMutationRespectsOwnershipAndSelfRemoval()
    {
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var lookup = new OASISResult<IClan>(new Clan { OwnerAvatarId = owner });

        Assert.Null(ClanController.GetClanMutationRefusal(lookup, owner, isWizard: false));
        Assert.Null(ClanController.GetClanMutationRefusal(lookup, member, isWizard: true));
        Assert.Null(ClanController.GetClanMutationRefusal(lookup, member, isWizard: false, leavingAvatarId: member));
        Assert.Equal(HttpStatusCode.Unauthorized,
            ClanController.GetClanMutationRefusal(lookup, member, isWizard: false, leavingAvatarId: owner).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            ClanController.GetClanMutationRefusal(lookup, Guid.Empty, isWizard: true).StatusCode);
    }
}
