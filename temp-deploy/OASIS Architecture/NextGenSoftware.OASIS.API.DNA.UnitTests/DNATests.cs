using NextGenSoftware.OASIS.API.DNA;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.DNA.UnitTests
{
    public class DNATests
    {
        [Fact]
        public void OASISDNA_DefaultConstructor_ShouldInitializeCorrectly()
        {
            // Act
            var dna = new OASISDNA();

            // Assert
            dna.Should().NotBeNull();
        }

        [Fact]
        public void OASISDNA_WithOASISProperty_ShouldSetPropertyCorrectly()
        {
            // Arrange
            var expectedOASIS = new OASIS();

            // Act
            var dna = new OASISDNA
            {
                OASIS = expectedOASIS
            };

            // Assert
            dna.OASIS.Should().Be(expectedOASIS);
        }

        [Fact]
        public void OASISDNA_ShouldHaveOASISProperty()
        {
            // Act
            var dna = new OASISDNA();

            // Assert
            dna.Should().HaveProperty("OASIS");
        }

        [Fact]
        public void OASIS_DefaultConstructor_ShouldInitializeCorrectly()
        {
            // Act
            var oasis = new OASIS();

            // Assert
            oasis.Should().NotBeNull();
        }
    }
}
