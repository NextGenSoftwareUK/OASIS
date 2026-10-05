using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Helpers;

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

        [Fact]
        public void CopyWithoutInnerResult_PreservesCompleteOperationDiagnostics()
        {
            var source = new OASISResult<string>
            {
                IsError = true,
                IsWarning = true,
                IsSaved = true,
                IsLoaded = true,
                IsDeleted = true,
                ErrorCode = "PROVIDER_CONTRACT_ERROR",
                Message = "message",
                DetailedMessage = "details",
                ResultsCount = 11,
                ErrorCount = 2,
                WarningCount = 3,
                SavedCount = 4,
                LoadedCount = 5,
                DeletedCount = 6,
                HasAnyHolonsChanged = true,
                InnerMessages = new System.Collections.Generic.List<string> { "inner" },
                StackTraces = new System.Collections.Generic.List<string> { "stack" },
                MetaData = new System.Collections.Generic.Dictionary<string, string> { ["provider"] = "test" }
            };

            var copy = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<string, int>(source);

            copy.Should().BeEquivalentTo(source, options => options.Excluding(x => x.Result));
            copy.Result.Should().Be(0);
        }
    }
}

