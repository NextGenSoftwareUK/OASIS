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
    public class InventoryItemsControllerTests
    {
        private readonly Mock<ILogger<InventoryItemsController>> _mockLogger;
        private readonly InventoryItemsController _controller;

        public InventoryItemsControllerTests()
        {
            _mockLogger = new Mock<ILogger<InventoryItemsController>>();
            _controller = new InventoryItemsController();
        }

        [Fact]
        public async Task GetAllInventoryItems_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllInventoryItems();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetInventoryItem_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetInventoryItem(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateInventoryItem_WithValidItem_ShouldReturnOASISResult()
        {
            // Arrange
            var mockItem = new Mock<IInventoryItem>();
            mockItem.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockItem.Setup(x => x.Name).Returns("Test Inventory Item");

            // Act
            var result = await _controller.CreateInventoryItem(mockItem.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateInventoryItem_WithValidIdAndItem_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockItem = new Mock<IInventoryItem>();
            mockItem.Setup(x => x.Id).Returns(id);
            mockItem.Setup(x => x.Name).Returns("Updated Inventory Item");

            // Act
            var result = await _controller.UpdateInventoryItem(id, mockItem.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeleteInventoryItem_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteInventoryItem(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}
