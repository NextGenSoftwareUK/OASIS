using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class HolonsControllerTests
    {
        private readonly HolonsController _controller;

        public HolonsControllerTests()
        {
            _controller = new HolonsController();
            STARControllerTestHelper.SetUpControllerContext(_controller);
        }

        [Fact]
        public async Task GetAllHolons_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllHolons();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task GetHolon_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetHolon(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task CreateHolon_WithValidHolon_ShouldReturnOASISResult()
        {
            // Arrange
            var holon = new STARHolon { Id = Guid.NewGuid(), Name = "Test Holon" };

            // Act
            var result = await _controller.CreateHolon(holon);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task UpdateHolon_WithValidIdAndHolon_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var holon = new STARHolon { Id = id, Name = "Updated Holon" };

            // Act
            var result = await _controller.UpdateHolon(id, holon);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task DeleteHolon_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteHolon(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }
    }
}
