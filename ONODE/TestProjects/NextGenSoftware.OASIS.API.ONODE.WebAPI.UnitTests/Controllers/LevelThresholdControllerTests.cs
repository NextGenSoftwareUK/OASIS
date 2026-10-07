using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Controllers
{
    public class LevelThresholdControllerTests
    {
        [Fact]
        public void KarmaAndAvatarApisExposeTheSameEscalatingThresholds()
        {
            var karmaResult = new KarmaController().GetLevelThresholds();
            var avatarResult = new AvatarProfileController(
                Mock.Of<ILogger<AvatarProfileController>>(),
                Mock.Of<IConfiguration>(),
                Mock.Of<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()).GetLevelThresholds();

            Assert.False(karmaResult.IsError);
            Assert.False(avatarResult.IsError);
            Assert.Equal(karmaResult.Result, avatarResult.Result);
            Assert.Equal(101, karmaResult.Result[2]);
            Assert.Equal(226, karmaResult.Result[3]);
            Assert.Equal(382, karmaResult.Result[4]);
            Assert.True(karmaResult.Result[4] - karmaResult.Result[3] >
                        karmaResult.Result[3] - karmaResult.Result[2]);
        }
    }
}
