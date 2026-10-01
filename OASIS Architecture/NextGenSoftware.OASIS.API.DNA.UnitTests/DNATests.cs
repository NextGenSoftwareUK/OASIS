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
            dna.OASIS.Should().NotBeNull("a default DNA object must be safe for configuration consumers");
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
            var prop = typeof(OASISDNA).GetProperty("OASIS");

            // Assert
            prop.Should().NotBeNull();
            prop!.GetValue(dna).Should().Be(dna.OASIS);
        }

        [Fact]
        public void OASIS_DefaultConstructor_ShouldInitializeCorrectly()
        {
            // Act
            var oasis = new OASIS();

            // Assert
            oasis.Should().NotBeNull();
        }

        [Fact]
        public void HyperDriveOfflineSync_ShouldBeEnabledByDefault()
        {
            new NextGenSoftware.OASIS.API.Core.Configuration.OASISHyperDriveConfig()
                .OfflineSyncEnabled.Should().BeTrue();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LoadDNA_ShouldHonorExplicitOfflineSyncSetting(bool enabled)
        {
            var path = Path.Combine(Path.GetTempPath(), $"oasis-dna-{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(path,
                    $"{{\"OASIS\":{{\"OASISHyperDriveConfig\":{{\"OfflineSyncEnabled\":{enabled.ToString().ToLowerInvariant()}}}}}}}");

                var result = OASISDNAManager.LoadDNA(path);

                result.IsError.Should().BeFalse(result.Message);
                result.Result!.OASIS.OASISHyperDriveConfig.OfflineSyncEnabled.Should().Be(enabled);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void LoadDNA_ShouldRejectUnknownHyperDriveMode()
        {
            var path = Path.Combine(Path.GetTempPath(), $"oasis-dna-{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(path, "{\"OASIS\":{\"HyperDriveMode\":\"TypoMode\"}}");

                var result = OASISDNAManager.LoadDNA(path);

                result.IsError.Should().BeTrue();
                result.Message.Should().Contain("Unsupported HyperDriveMode");
                OASISDNAManager.OASISDNA.Should().BeNull();
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Theory]
        [InlineData(HyperDriveModes.Legacy)]
        [InlineData(HyperDriveModes.V2)]
        public void LoadDNA_ShouldAcceptDocumentedHyperDriveModes(string mode)
        {
            var path = Path.Combine(Path.GetTempPath(), $"oasis-dna-{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(path, $"{{\"OASIS\":{{\"HyperDriveMode\":\"{mode}\"}}}}");

                var result = OASISDNAManager.LoadDNA(path);

                result.IsError.Should().BeFalse(result.Message);
                result.Result!.OASIS.HyperDriveMode.Should().Be(mode);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void LoadDNA_ShouldRejectCapabilityRegistryQuorumLargerThanRegistrySet()
        {
            var path = Path.Combine(Path.GetTempPath(), $"oasis-dna-{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(path, "{\"OASIS\":{\"ONET\":{\"CapabilityRegistryNodeIds\":[\"registry-a\"],\"CapabilityRegistryQuorum\":2}}}");
                var result = OASISDNAManager.LoadDNA(path);
                result.IsError.Should().BeTrue();
                result.Message.Should().Contain("CapabilityRegistryQuorum");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
    }
}
