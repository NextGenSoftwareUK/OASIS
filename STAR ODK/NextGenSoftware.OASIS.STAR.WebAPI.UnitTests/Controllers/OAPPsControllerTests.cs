using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class OAPPsControllerTests
    {
        private readonly OAPPsController _controller;

        public OAPPsControllerTests()
        {
            _controller = new OAPPsController();
            STARControllerTestHelper.SetUpControllerContext(_controller);
        }

        [Fact]
        public async Task GetAllOAPPs_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllOAPPs();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
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
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task CreateOAPP_WithValidOAPP_ShouldReturnOASISResult()
        {
            // Arrange
            var oapp = new OAPP { Id = Guid.NewGuid(), Name = "Test OAPP" };

            // Act
            var result = await _controller.CreateOAPP(oapp);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task UpdateOAPP_WithValidIdAndOAPP_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var oapp = new OAPP { Id = id, Name = "Updated OAPP" };

            // Act
            var result = await _controller.UpdateOAPP(id, oapp);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
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
            result.Should().BeAssignableTo<IActionResult>();
        }
    }
}
