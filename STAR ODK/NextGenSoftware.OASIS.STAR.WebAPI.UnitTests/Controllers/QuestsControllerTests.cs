using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

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
            STARControllerTestHelper.SetUpControllerContext(_controller);
        }

        [Fact]
        public async Task GetAllQuests_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllIQuests();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task GetQuest_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetIQuest(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task CreateQuest_WithValidQuest_ShouldReturnOASISResult()
        {
            // Arrange
            var mockQuest = new Mock<IQuest>();
            mockQuest.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockQuest.Setup(x => x.Name).Returns("Test Quest");

            // Act
            var result = await _controller.CreateIQuest(mockQuest.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
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
            var result = await _controller.UpdateIQuest(id, mockQuest.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task DeleteQuest_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteIQuest(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task GetQuestsByStatus_WithValidStatus_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetQuestsByStatus("InProgress");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task GetQuestsByStatus_WithNullStatus_ShouldReturnBadRequest()
        {
            // Act - pass null (caller may send missing route value)
            var result = await _controller.GetQuestsByStatus(null);

            // Assert
            result.Should().NotBeNull();
            var badRequest = result as BadRequestObjectResult;
            badRequest.Should().NotBeNull();
        }

        [Fact]
        public async Task GetQuestsByStatus_WithEmptyStatus_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.GetQuestsByStatus("");

            // Assert
            result.Should().NotBeNull();
            var badRequest = result as BadRequestObjectResult;
            badRequest.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateQuestWithOptions_WithObjectives_ShouldReturnOASISResult()
        {
            var request = new CreateQuestRequest
            {
                Name = "Unit Test Quest",
                Description = "Quest with objectives",
                HolonSubType = HolonType.Quest,
                Objectives = new List<QuestObjectiveRequest>
                {
                    new QuestObjectiveRequest { Name = "Obj1", Description = "First objective", GameSource = "ODOOM", ItemRequired = "Key", Order = 0 },
                    new QuestObjectiveRequest { Name = "Obj2", Description = "Second objective", GameSource = "OQUAKE", ItemRequired = "Health", Order = 1 }
                }
            };

            var result = await _controller.CreateQuestWithOptions(request);

            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task AddQuestObjective_WithValidRequest_ShouldReturnOASISResult()
        {
            var questId = Guid.NewGuid();
            var request = new AddQuestObjectiveRequest { Name = "New Obj", Description = "Objective desc", GameSource = "ODOOM", ItemRequired = "Key", Order = 0 };

            var result = await _controller.AddQuestObjective(questId, request);

            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task RemoveQuestObjective_WithValidIds_ShouldReturnOASISResult()
        {
            var parentId = Guid.NewGuid();
            var objectiveId = Guid.NewGuid();

            var result = await _controller.RemoveQuestObjective(parentId, objectiveId);

            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task AddSubQuest_WithValidRequest_ShouldReturnOASISResult()
        {
            var questId = Guid.NewGuid();
            var request = new AddSubQuestRequest { Name = "Sub", Description = "Sub-quest desc", GameSource = "ODOOM", ItemRequired = "Level", Order = 0 };

            var result = await _controller.AddSubQuest(questId, request);

            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task RemoveSubQuest_WithValidIds_ShouldReturnOASISResult()
        {
            var parentId = Guid.NewGuid();
            var subQuestId = Guid.NewGuid();

            var result = await _controller.RemoveSubQuest(parentId, subQuestId);

            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }
    }
}
