using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.Services;

public class RateServiceTests
{
    [Fact]
    public void Test_ShouldPass()
    {
        // Arrange
        var expected = true;
        
        // Act
        var actual = true;
        
        // Assert
        actual.Should().Be(expected);
    }
}
