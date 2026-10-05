using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.Common.UnitTests
{
    public class OASISResultTests
    {
        [Fact]
        public void OASISResult_DefaultConstructor_ShouldInitializeCorrectly()
        {
            // Act
            var result = new OASISResult<string>();

            // Assert
            result.Should().NotBeNull();
            result.IsError.Should().BeFalse();
            result.Message.Should().BeNullOrEmpty();
            result.Result.Should().BeNull();
        }

        [Fact]
        public void OASISResult_WithResult_ShouldSetResultCorrectly()
        {
            // Arrange
            var expectedResult = "Test Result";

            // Act
            var result = new OASISResult<string>(expectedResult);

            // Assert
            result.Result.Should().Be(expectedResult);
            result.IsError.Should().BeFalse();
        }

        [Fact]
        public void OASISResult_WithError_ShouldSetErrorCorrectly()
        {
            // Arrange
            var errorMessage = "Test Error";

            // Act
            var result = new OASISResult<string>
            {
                IsError = true,
                Message = errorMessage
            };

            // Assert
            result.IsError.Should().BeTrue();
            result.Message.Should().Be(errorMessage);
        }

        [Fact]
        public void OASISResult_WithException_ShouldSetExceptionCorrectly()
        {
            // Arrange
            var exception = new InvalidOperationException("Test Exception");

            // Act
            var result = new OASISResult<string>
            {
                IsError = true,
                Exception = exception
            };

            // Assert
            result.IsError.Should().BeTrue();
            result.Exception.Should().Be(exception);
        }

        [Fact]
        public void OASISResult_WithCollection_ShouldHandleCollectionsCorrectly()
        {
            // Arrange
            var items = new List<string> { "Item1", "Item2", "Item3" };

            // Act
            var result = new OASISResult<IEnumerable<string>>(items);

            // Assert
            result.Result.Should().NotBeNull();
            result.Result.Should().HaveCount(3);
            result.Result.Should().Contain("Item1");
            result.Result.Should().Contain("Item2");
            result.Result.Should().Contain("Item3");
        }

        [Fact]
        public void OASISResult_WithNullResult_ShouldHandleNullCorrectly()
        {
            // Act
            var result = new OASISResult<string?>(null);

            // Assert
            result.Result.Should().BeNull();
            result.IsError.Should().BeFalse();
        }
    }
}
