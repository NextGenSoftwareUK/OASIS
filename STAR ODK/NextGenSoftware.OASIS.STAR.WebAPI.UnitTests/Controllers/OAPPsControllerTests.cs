using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class OAPPsControllerTests
    {
        private readonly Mock<ILogger<OAPPsController>> _mockLogger;
        private readonly OAPPsController _controller;

        public OAPPsControllerTests()
        {
            _mockLogger = new Mock<ILogger<OAPPsController>>();
            _controller = new OAPPsController();
        }

        [Fact]
        public async Task GetAllOAPPs_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllOAPPs();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetOAPP_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetOAPP(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateOAPP_WithValidOAPP_ShouldReturnOASISResult()
        {
            // Arrange
            var mockOAPP = new Mock<IOAPP>();
            mockOAPP.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockOAPP.Setup(x => x.Name).Returns("Test OAPP");

            // Act
            var result = await _controller.CreateOAPP(mockOAPP.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateOAPP_WithValidIdAndOAPP_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockOAPP = new Mock<IOAPP>();
            mockOAPP.Setup(x => x.Id).Returns(id);
            mockOAPP.Setup(x => x.Name).Returns("Updated OAPP");

            // Act
            var result = await _controller.UpdateOAPP(id, mockOAPP.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeleteOAPP_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteOAPP(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}
