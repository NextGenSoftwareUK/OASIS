using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class SharingRequestAdmissionTests
{
    [Fact]
    public async Task MissingHolonPermissionBodyRejectsBeforeProviderActivation()
    {
        var result = await new ShareController().SetPermissions(Guid.NewGuid(), null!);
        Assert.True(result.IsError);
        Assert.Equal("COSMIC_PERMISSIONS_INPUT_INVALID", result.ErrorCode);
        Assert.Equal("A request body is required.", result.Message);
        Assert.Null(result.Exception);
    }

    [Theory]
    [InlineData("avatar")]
    [InlineData("detail")]
    public async Task MissingAvatarSharingBodyRejectsBeforeAvatarOrProviderAccess(string target)
    {
        var controller = new AvatarProfileController(null!, null!, null!);
        var result = await controller.SetSharing(target, null!);
        Assert.True(result.IsError);
        Assert.Equal("COSMIC_PERMISSIONS_INPUT_INVALID", result.ErrorCode);
        Assert.Equal("A request body is required.", result.Message);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task InvalidPermissionNamesRemainInputErrorsBeforeProviderAccess()
    {
        var request = new SetPermissionsRequest
        {
            Grants = new() { ["public"] = new[] { "not-a-permission" } }
        };
        var result = await new ShareController().SetPermissions(Guid.NewGuid(), request);
        Assert.True(result.IsError);
        Assert.Equal("COSMIC_PERMISSIONS_INPUT_INVALID", result.ErrorCode);
        Assert.Contains("Invalid permission names", result.Message);
        Assert.Null(result.Exception);
    }
}
