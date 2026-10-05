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
    public class QuestsControllerTests
    {
        private readonly Mock<ILogger<QuestsController>> _mockLogger;
        private readonly QuestsController _controller;

        public QuestsControllerTests()
        {
            _mockLogger = new Mock<ILogger<QuestsController>>();
            _controller = new QuestsController();
        }

        [Fact]
        public async Task GetAllQuests_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllQuests();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetQuest_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetQuest(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateQuest_WithValidQuest_ShouldReturnOASISResult()
        {
            // Arrange
            var mockQuest = new Mock<IQuest>();
            mockQuest.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockQuest.Setup(x => x.Name).Returns("Test Quest");

            // Act
            var result = await _controller.CreateQuest(mockQuest.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateQuest_WithValidIdAndQuest_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockQuest = new Mock<IQuest>();
            mockQuest.Setup(x => x.Id).Returns(id);
            mockQuest.Setup(x => x.Name).Returns("Updated Quest");

            // Act
            var result = await _controller.UpdateQuest(id, mockQuest.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeleteQuest_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteQuest(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}
