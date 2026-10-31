using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task EncryptedConditionalSaveRejectsMissingPreparationCapabilityBeforeStorage(bool synchronous)
    {
        var f = Fixture(true, conditional: true); f.Manager.MetadataEncryptionPolicy = MetadataPolicy(new MetadataResolver());
        var source = new Holon { MetaData = new() { ["private-note"] = "private" } };
        var result = synchronous ? f.Manager.SaveHolon(source, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false,
            providerType: ProviderType.SQLLiteDBOASIS) : await f.Manager.SaveHolonAsync(source, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()),
            saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        result.ErrorCode.Should().Be("COSMIC_PREPARATION_UNSUPPORTED"); source.Id.Should().BeEmpty(); source.MetaData["private-note"].Should().Be("private");
        f.Provider.As<IConditionalHolonStorageProvider>().Verify(provider => provider.ExecuteConditionalHolonWrite(It.IsAny<ConditionalHolonWriteRequest>()), Times.Never);
        f.Provider.As<IConditionalHolonStorageProvider>().Verify(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OlderEncryptedConditionalSaveRequiresAnExplicitPreparationContext(bool synchronous)
    {
        var f = Fixture(true, conditional: true, preparation: true);
        f.Manager.OASISDNA.OASIS.Security = new SecuritySettings { HolonDataEncryption = new EncryptionSettings {
            Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('e', 32) } };
        var source = new Holon { MetaData = new() { ["private-note"] = "private" } };
        var result = synchronous ? f.Manager.SaveHolon(source, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false,
            providerType: ProviderType.SQLLiteDBOASIS) : await f.Manager.SaveHolonAsync(source, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()),
            saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("HOLON_SAVE_PREPARATION_FAILED"); source.Id.Should().BeEmpty();
        f.Provider.As<IHolonWritePreparationStorageProvider>().Verify(provider => provider.ClaimHolonWritePreparation(It.IsAny<HolonWritePreparation>()), Times.Never);
        f.Provider.As<IHolonWritePreparationStorageProvider>().Verify(provider => provider.ClaimHolonWritePreparationAsync(It.IsAny<HolonWritePreparation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task KnownPreparationCleanupWarningsSurviveTheSubsequentCommittedWrite(bool synchronous)
    {
        var f = Fixture(true, conditional: true, preparation: true); f.Manager.MetadataEncryptionPolicy = MetadataPolicy(new MetadataResolver());
        OASISResult<HolonWritePreparation> Claim(HolonWritePreparation candidate) => new(candidate) { IsWarning = true, WarningCount = 1,
            Message = "known claim cleanup", Exception = new IOException("claim cleanup") };
        f.Provider.As<IHolonWritePreparationStorageProvider>().Setup(provider => provider.ClaimHolonWritePreparation(It.IsAny<HolonWritePreparation>()))
            .Returns((HolonWritePreparation candidate) => Claim(candidate));
        f.Provider.As<IHolonWritePreparationStorageProvider>().Setup(provider => provider.ClaimHolonWritePreparationAsync(It.IsAny<HolonWritePreparation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HolonWritePreparation candidate, CancellationToken token) => Claim(candidate));
        OASISResult<IEnumerable<IHolon>> Write(ConditionalHolonWriteRequest request)
        {
            var root = request.Roots.Single(); root.VersionId = Guid.NewGuid(); root.Version = 1;
            CosmicConcurrency.SetProviderRevisionId(root, ProviderType.SQLLiteDBOASIS, Guid.NewGuid()); return new(request.Roots);
        }
        f.Provider.As<IConditionalHolonStorageProvider>().Setup(provider => provider.ExecuteConditionalHolonWrite(It.IsAny<ConditionalHolonWriteRequest>()))
            .Returns((ConditionalHolonWriteRequest request) => Write(request));
        f.Provider.As<IConditionalHolonStorageProvider>().Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConditionalHolonWriteRequest request, CancellationToken token) => Write(request));
        var source = new Holon { MetaData = new() { ["private-note"] = "private" } };
        var result = synchronous ? f.Manager.SaveHolon(source, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false,
            providerType: ProviderType.SQLLiteDBOASIS) : await f.Manager.SaveHolonAsync(source, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()),
            saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.IsSaved.Should().BeTrue(); result.IsWarning.Should().BeTrue();
        result.Exception.Should().BeOfType<IOException>(); source.Id.Should().NotBeEmpty(); source.MetaData["private-note"].ToString().Should().Be("private");
    }
}
