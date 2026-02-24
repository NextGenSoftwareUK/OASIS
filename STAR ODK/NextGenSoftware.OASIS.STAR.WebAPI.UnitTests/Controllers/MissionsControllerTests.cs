using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class MissionsControllerTests
    {
        private readonly Mock<ILogger<MissionsController>> _mockLogger;
        private readonly MissionsController _controller;

        public MissionsControllerTests()
        {
            _mockLogger = new Mock<ILogger<MissionsController>>();
            _controller = new MissionsController();
            STARControllerTestHelper.SetUpControllerContext(_controller);
        }

        [Fact]
        public async Task GetAllMissions_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllMissions();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task GetMission_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetMission(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task CreateMission_WithValidMission_ShouldReturnOASISResult()
        {
            // Arrange
            var mockMission = new Mock<IMission>();
            mockMission.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockMission.Setup(x => x.Name).Returns("Test Mission");

            // Act
            var result = await _controller.CreateMission(mockMission.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task UpdateMission_WithValidIdAndMission_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockMission = new Mock<IMission>();
            mockMission.Setup(x => x.Id).Returns(id);
            mockMission.Setup(x => x.Name).Returns("Updated Mission");

            // Act
            var result = await _controller.UpdateMission(id, mockMission.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task DeleteMission_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteMission(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }
    }
}
