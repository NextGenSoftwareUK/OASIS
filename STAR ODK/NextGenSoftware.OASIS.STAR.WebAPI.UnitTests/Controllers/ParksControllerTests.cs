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
    public class ParksControllerTests
    {
        private readonly Mock<ILogger<ParksController>> _mockLogger;
        private readonly ParksController _controller;

        public ParksControllerTests()
        {
            _mockLogger = new Mock<ILogger<ParksController>>();
            _controller = new ParksController();
        }

        [Fact]
        public async Task GetAllParks_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllParks();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetPark_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetPark(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreatePark_WithValidPark_ShouldReturnOASISResult()
        {
            // Arrange
            var mockPark = new Mock<IPark>();
            mockPark.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockPark.Setup(x => x.Name).Returns("Test Park");

            // Act
            var result = await _controller.CreatePark(mockPark.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdatePark_WithValidIdAndPark_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockPark = new Mock<IPark>();
            mockPark.Setup(x => x.Id).Returns(id);
            mockPark.Setup(x => x.Name).Returns("Updated Park");

            // Act
            var result = await _controller.UpdatePark(id, mockPark.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeletePark_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeletePark(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}
