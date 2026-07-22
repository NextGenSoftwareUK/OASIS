using NextGenSoftware.OASIS.API.ONODE.WebAPI;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests
{
    public class ONODEWebAPITests
    {
        [Fact]
        public void ONODEWebAPI_DefaultConstructor_ShouldInitializeCorrectly()
        {
            // Act
            var webAPI = new ONODEWebAPI();

            // Assert
            webAPI.Should().NotBeNull();
        }

        [Fact]
        public void ONODEWebAPI_ShouldHaveRequiredProperties()
        {
            // Act
            var webAPI = new ONODEWebAPI();

            // Assert
            webAPI.Should().NotBeNull();
            // Add more specific property tests based on actual ONODEWebAPI implementation
        }

        [Fact]
        public void ONODEWebAPI_ShouldBePublicClass()
        {
            // Act
            var webAPIType = typeof(ONODEWebAPI);

            // Assert
            webAPIType.IsPublic.Should().BeTrue();
        }

        [Fact]
        public void ONODEWebAPI_ShouldHaveEndpointNameConstant()
        {
            // Arrange & Act
            var endpointName = ONODEWebAPI.EndpointName;

            // Assert
            endpointName.Should().Be("ONODE", "ONODEWebAPI identifies as the ONODE endpoint");
        }
    }
}
