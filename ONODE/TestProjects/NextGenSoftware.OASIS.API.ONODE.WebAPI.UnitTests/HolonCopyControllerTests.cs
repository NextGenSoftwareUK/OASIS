using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;
using CoreAvatar = NextGenSoftware.OASIS.API.Core.Holons.Avatar;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class HolonCopyControllerTests
{
    [Theory]
    [InlineData(nameof(DataController.GetHolonCopyProviders), null, 401)]
    [InlineData(nameof(DataController.GetHolonCopyProviders), AvatarType.User, 403)]
    [InlineData(nameof(DataController.GetHolonCopyProviders), AvatarType.Wizard, null)]
    [InlineData(nameof(DataController.CopyHolon), null, 401)]
    [InlineData(nameof(DataController.CopyHolon), AvatarType.User, 403)]
    [InlineData(nameof(DataController.CopyHolon), AvatarType.Wizard, null)]
    public void CopyRoutesUseWizardAuthorization(string action, AvatarType? avatarType, int? expectedStatus)
    {
        var filter = typeof(DataController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(filter);
        var http = new DefaultHttpContext();
        if (avatarType.HasValue)
            http.Items["Avatar"] = new CoreAvatar { AvatarType = new EnumValue<AvatarType>(avatarType.Value) };
        var context = new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>());

        filter.OnAuthorization(context);

        if (expectedStatus.HasValue)
            Assert.Equal(expectedStatus, Assert.IsType<JsonResult>(context.Result).StatusCode);
        else
            Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("Default", "MongoDBOASIS")]
    [InlineData("SQLLiteDBOASIS", "All")]
    [InlineData("SQLLiteDBOASIS", "sqllitedboasis")]
    [InlineData("NotAProvider", "MongoDBOASIS")]
    [InlineData("1", "MongoDBOASIS")]
    public async Task InvalidExplicitProvidersAreRejectedBeforeStorageResolution(string source, string destination)
    {
        var controller = new DataController();

        var response = await controller.CopyHolon(new DataController.HolonCopyRequest(Guid.NewGuid(), source, destination));

        var failure = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.True(Assert.IsType<OASISResult<Guid>>(failure.Value).IsError);
    }
}
