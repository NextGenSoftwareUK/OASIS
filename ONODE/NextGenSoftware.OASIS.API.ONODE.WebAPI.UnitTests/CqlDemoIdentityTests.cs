using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlDemoIdentityTests
{
    [Fact]
    public void MappingReusesCoreIdentityAndSeparatesOwnersWithoutProviderIo()
    {
        var owner = Guid.NewGuid();
        var request = new CqlDemoIdentityRequest { Provider = "ActivityPubOASIS", Keys = new[] { "101", "102" } };
        var providers = new Mock<ICqlV1Providers>(MockBehavior.Strict);
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("validated-test")) };
        http.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = owner };
        var controller = new CqlV1Controller(providers.Object) { ControllerContext = new ControllerContext { HttpContext = http } };
        var response = Assert.IsType<OASISResult<object>>(Assert.IsType<OkObjectResult>(controller.DemoIdentities(request)).Value);
        var result = Assert.IsType<CqlDemoIdentityResult>(response.Result);
        Assert.Equal(owner, result.OwnerId);
        Assert.Equal(NativeRecordHolonMapper.StableHolonId(owner, ProviderType.ActivityPubOASIS,
            CqlHostedDemoIdentities.SourceName, "101"), result.Records[0].Id);
        Assert.Equal(result.Records[0].Id, CqlHostedDemoIdentities.Create(request, owner).Records[0].Id);
        Assert.NotEqual(result.Records[0].Id, CqlHostedDemoIdentities.Create(request, Guid.NewGuid()).Records[0].Id);
        Assert.Equal("101", result.Records[0].Metadata[NativeRecordHolonMapper.KeyMetaDataKey]);
        providers.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("missing-avatar")]
    public void MappingRequiresTheAuthenticatedAvatarContext(string fault)
    {
        var http = new DefaultHttpContext();
        if (fault != "anonymous") http.User = new ClaimsPrincipal(new ClaimsIdentity("validated-test"));
        else http.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = Guid.NewGuid() };
        var controller = new CqlV1Controller(null) { ControllerContext = new ControllerContext { HttpContext = http } };
        Assert.IsType<UnauthorizedObjectResult>(controller.DemoIdentities(new CqlDemoIdentityRequest()));
    }

    [Theory]
    [InlineData("count")]
    [InlineData("duplicate")]
    [InlineData("size")]
    [InlineData("characters")]
    [InlineData("provider")]
    public void IdentityInputIsBoundedAndCannotSelectAnImplicitProvider(string fault)
    {
        var request = new CqlDemoIdentityRequest { Provider = "ActivityPubOASIS", Keys = new[] { "101" } };
        switch (fault)
        {
            case "count": request.Keys = new[] { "1", "2", "3" }; break;
            case "duplicate": request.Keys = new[] { "1", "1" }; break;
            case "size": request.Keys = new[] { new string('x', 129) }; break;
            case "characters": request.Keys = new[] { "bad\nkey" }; break;
            case "provider": request.Provider = "Default"; break;
        }
        Assert.Throws<ArgumentException>(() => CqlHostedDemoIdentities.Create(request, Guid.NewGuid()));
    }

    [Fact]
    public void CallerCannotSupplyAnotherOwnerInTheWireRequest()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CqlDemoIdentityRequest>(
            """{"provider":"ActivityPubOASIS","keys":["101"],"ownerId":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
