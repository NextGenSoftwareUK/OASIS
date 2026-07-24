using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class InventoryItemsControllerTests
    {
        private readonly InventoryItemsController _controller;

        public InventoryItemsControllerTests()
        {
            _controller = new InventoryItemsController();
            STARControllerTestHelper.SetUpControllerContext(_controller);
        }

        [Fact]
        public async Task GetAllInventoryItems_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllInventoryItems();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
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
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task CreateInventoryItem_WithValidItem_ShouldReturnOASISResult()
        {
            // Arrange
            var item = new InventoryItem { Id = Guid.NewGuid(), Name = "Test Inventory Item" };

            // Act
            var result = await _controller.CreateInventoryItem(item);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task UpdateInventoryItem_WithValidIdAndItem_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var item = new InventoryItem { Id = id, Name = "Updated Inventory Item" };

            // Act
            var result = await _controller.UpdateInventoryItem(id, item);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
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
            result.Should().BeAssignableTo<IActionResult>();
        }
    }
}
