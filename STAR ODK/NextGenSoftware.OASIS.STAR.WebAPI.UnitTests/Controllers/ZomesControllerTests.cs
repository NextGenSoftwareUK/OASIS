using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class ZomesControllerTests
    {
        private readonly ZomesController _controller;

        public ZomesControllerTests()
        {
            _controller = new ZomesController();
            STARControllerTestHelper.SetUpControllerContext(_controller);
        }

        [Fact]
        public async Task GetAllZomes_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllZomes();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task GetZome_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetZome(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task CreateZome_WithValidZome_ShouldReturnOASISResult()
        {
            // Arrange
            var zome = new STARZome { Id = Guid.NewGuid(), Name = "Test Zome" };

            // Act
            var result = await _controller.CreateZome(zome);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task UpdateZome_WithValidIdAndZome_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var zome = new STARZome { Id = id, Name = "Updated Zome" };

            // Act
            var result = await _controller.UpdateZome(id, zome);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task DeleteZome_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteZome(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }
    }
}
