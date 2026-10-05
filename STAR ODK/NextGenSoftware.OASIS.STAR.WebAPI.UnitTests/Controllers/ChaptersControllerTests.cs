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
    public class ChaptersControllerTests
    {
        private readonly Mock<ILogger<ChaptersController>> _mockLogger;
        private readonly ChaptersController _controller;

        public ChaptersControllerTests()
        {
            _mockLogger = new Mock<ILogger<ChaptersController>>();
            _controller = new ChaptersController();
        }

        [Fact]
        public async Task GetAllChapters_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllChapters();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetChapter_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetChapter(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateChapter_WithValidChapter_ShouldReturnOASISResult()
        {
            // Arrange
            var mockChapter = new Mock<IChapter>();
            mockChapter.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockChapter.Setup(x => x.Name).Returns("Test Chapter");

            // Act
            var result = await _controller.CreateChapter(mockChapter.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateChapter_WithValidIdAndChapter_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockChapter = new Mock<IChapter>();
            mockChapter.Setup(x => x.Id).Returns(id);
            mockChapter.Setup(x => x.Name).Returns("Updated Chapter");

            // Act
            var result = await _controller.UpdateChapter(id, mockChapter.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeleteChapter_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteChapter(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}
