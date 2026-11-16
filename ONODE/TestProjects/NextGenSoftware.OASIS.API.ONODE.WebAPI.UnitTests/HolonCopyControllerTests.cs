using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class HolonCopyControllerTests
{
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
