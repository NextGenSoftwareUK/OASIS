using NextGenSoftware.OASIS.API.Core.Exceptions;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests
{
    public class OASISExceptionTests
    {
        [Fact]
        public void OASISException_WithReason_ShouldCreateException()
        {
            // Arrange
            var reason = "Test reason";

            // Act
            var exception = new OASISException(reason);

            // Assert
            exception.Should().NotBeNull();
            exception.Should().BeOfType<OASISException>();
            exception.Reason.Should().Be(reason);
        }

        [Fact]
        public void OASISException_WithReason_ShouldSetReason()
        {
            // Arrange
            var reason = "Test OASIS Exception";

            // Act
            var exception = new OASISException(reason);

            // Assert
            exception.Reason.Should().Be(reason);
        }

        [Fact]
        public void OASISException_ShouldInheritFromException()
        {
            // Act
            var exception = new OASISException("Test reason");

            // Assert
            exception.Should().BeAssignableTo<Exception>();
        }
    }
}
