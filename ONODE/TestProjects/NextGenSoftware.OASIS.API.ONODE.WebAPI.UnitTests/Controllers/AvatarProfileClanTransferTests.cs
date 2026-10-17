using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Controllers
{
    public sealed class AvatarProfileClanTransferTests
    {
        [Fact]
        public async System.Threading.Tasks.Task AtomicRouteRejectsMissingClientIdempotencyIdentities()
        {
            var controller = new AvatarProfileController(
                Mock.Of<ILogger<AvatarProfileController>>(),
                Mock.Of<IConfiguration>(),
                Mock.Of<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.Avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar
                { AvatarId = Guid.NewGuid() };

            var response = await controller.SendItemToClanAtomic(new SendItemRequest
            {
                Target = "Edge Clan",
                ItemName = "Obsidian Pod",
                Quantity = 2
            });

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True(response.Result.IsError);
            Assert.Equal("CLAN_TRANSFER_IDEMPOTENCY_REQUIRED", response.Result.ErrorCode);
        }
    }
}
