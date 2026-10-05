using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.OASISBootLoader.UnitTests
{
    public class OASISBootLoaderTests
    {
        [Fact]
        public void OASISBootLoader_DefaultConstructor_ShouldInitializeCorrectly()
        {
            // Act
            var bootLoader = new OASISBootLoader();

            // Assert
            bootLoader.Should().NotBeNull();
        }

        [Fact]
        public void OASISBootLoader_ShouldHaveBootMethod()
        {
            // Arrange
            var bootLoader = new OASISBootLoader();

            // Act & Assert
            bootLoader.Should().HaveMethod("Boot", new Type[] { });
        }

        [Fact]
        public void OASISBootLoader_ShouldHaveShutdownMethod()
        {
            // Arrange
            var bootLoader = new OASISBootLoader();

            // Act & Assert
            bootLoader.Should().HaveMethod("Shutdown", new Type[] { });
        }

        [Fact]
        public void OASISBootLoader_ShouldBePublicClass()
        {
            // Act
            var bootLoaderType = typeof(OASISBootLoader);

            // Assert
            bootLoaderType.IsPublic.Should().BeTrue();
        }
    }
}
