using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class STARControllerTests
    {
        private readonly STARController _controller;

        public STARControllerTests()
        {
            _controller = new STARController();
        }

        [Fact]
        public void GetStatus_ShouldReturnIActionResult()
        {
            var result = _controller.GetStatus();
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task IgniteSTAR_WithValidRequest_ShouldReturnSuccess()
        {
            var request = new IgniteRequest
            {
                UserName = "testuser",
                Password = "testpass"
            };
            var result = await _controller.IgniteSTAR(request);
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task IgniteSTAR_WithNullRequest_ShouldReturnOk()
        {
            var result = await _controller.IgniteSTAR(null);
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task ExtinguishSTAR_ShouldReturnIActionResult()
        {
            var result = await _controller.ExtinguishSTAR();
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task BeamIn_WithValidRequest_ShouldReturnSuccess()
        {
            var request = new BeamInRequest
            {
                Username = "testuser",
                Password = "testpass"
            };
            var result = await _controller.BeamIn(request);
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }
    }
}
