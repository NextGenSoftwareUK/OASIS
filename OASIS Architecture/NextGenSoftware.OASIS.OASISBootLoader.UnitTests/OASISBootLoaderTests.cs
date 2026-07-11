using System.Reflection;
using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.OASISBootLoader.UnitTests
{
    public class OASISBootLoaderTests
    {
        [Fact]
        public void OASISBootLoader_Type_ShouldExist()
        {
            // Act
            var bootLoaderType = typeof(OASISBootLoader);

            // Assert
            bootLoaderType.Should().NotBeNull();
        }

        [Fact]
        public void OASISBootLoader_ShouldHaveBootMethod()
        {
            // Arrange
            var bootLoaderType = typeof(OASISBootLoader);
            var method = bootLoaderType.GetMethod("Boot", Type.EmptyTypes);

            // Assert
            method.Should().NotBeNull();
        }

        [Fact]
        public void OASISBootLoader_ShouldHaveShutdownMethod()
        {
            // Arrange
            var bootLoaderType = typeof(OASISBootLoader);
            var method = bootLoaderType.GetMethod("Shutdown", Type.EmptyTypes);

            // Assert
            method.Should().NotBeNull();
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
