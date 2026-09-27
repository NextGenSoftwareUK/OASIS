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
        public void OASISBootLoader_ShouldExposeBootOASISMethod()
        {
            // Arrange
            var bootLoaderType = typeof(OASISBootLoader);
            var method = bootLoaderType.GetMethod(
                nameof(OASISBootLoader.BootOASIS),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(bool) },
                modifiers: null);

            // Assert
            method.Should().NotBeNull();
        }

        [Fact]
        public void OASISBootLoader_ShouldExposeShutdownOASISMethod()
        {
            // Arrange
            var bootLoaderType = typeof(OASISBootLoader);
            var method = bootLoaderType.GetMethod(
                nameof(OASISBootLoader.ShutdownOASIS),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

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
