using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class CelestialSpacesControllerTests
    {
        private readonly CelestialSpacesController _controller;

        public CelestialSpacesControllerTests()
        {
            _controller = new CelestialSpacesController();
        }

        [Fact]
        public async Task GetAllCelestialSpaces_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllCelestialSpaces();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetCelestialSpace_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetCelestialSpace(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateCelestialSpace_WithValidSpace_ShouldReturnOASISResult()
        {
            // Arrange
            var space = new STARCelestialSpace { Id = Guid.NewGuid(), Name = "Test Celestial Space" };

            // Act
            var result = await _controller.CreateCelestialSpace(space);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateCelestialSpace_WithValidIdAndSpace_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var space = new STARCelestialSpace { Id = id, Name = "Updated Celestial Space" };

            // Act
            var result = await _controller.UpdateCelestialSpace(id, space);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeleteCelestialSpace_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteCelestialSpace(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}
