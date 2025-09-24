using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests
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
    }
}

