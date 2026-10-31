using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class HostedConditionalDeleteAuthorityTests
{
    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, true, true)]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, false, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, false, true)]
    public async Task DeleteUsesCapturedAuthenticatedRoleActorAndOriginalCondition(
        bool wizard, bool byKey, bool mismatchedIdentity, bool soft)
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var operation = Guid.NewGuid();
        var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = actor,
            AvatarType = new EnumValue<AvatarType>(wizard ? AvatarType.Wizard : AvatarType.User) };
        var context = new DefaultHttpContext { RequestServices = services };
        context.Items["Avatar"] = avatar;
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        var provider = new Mock<IOASISStorageProvider>();
        provider.As<IConditionalHolonStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.IsProviderActivated = true;
        provider.Setup(value => value.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var stored = new Holon { Id = mismatchedIdentity ? Guid.NewGuid() : id,
            CreatedByAvatarId = Guid.NewGuid() };
        CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        Action mutateCaller = () =>
        {
            avatar.Id = Guid.NewGuid();
            avatar.AvatarType = new EnumValue<AvatarType>(wizard ? AvatarType.User : AvatarType.Wizard);
        };
        provider.Setup(value => value.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .Callback(mutateCaller).ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        provider.Setup(value => value.LoadHolonAsync("native-key", false, true, 0, true, false, 0))
            .Callback(mutateCaller).ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        ConditionalHolonWriteRequest? submitted = null;
        provider.As<IConditionalHolonStorageProvider>()
            .Setup(value => value.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true,
                ErrorCode = CosmicConcurrencyException.Code, Message = "Controlled original-revision conflict" });
        var registry = new ProviderManager(null, dna);
        registry.RegisterProvider(provider.Object);
        Assert.False(registry.SetAndActivateCurrentStorageProvider(provider.Object).IsError);
        var manager = new HolonManager(provider.Object, dna, registry);

        var result = await HostedHolonWriteEndpoint.DeleteAsync(context, HyperDriveModes.V2,
            new DeleteHolonRequest { Id = id, ProviderKey = byKey ? "native-key" : null!,
                SoftDelete = soft, OperationId = operation, ProviderType = "SQLLiteDBOASIS",
                ExpectedRevisions = new Dictionary<Guid, Guid?> { [id] = revision } }, () => manager);

        Assert.Equal(mismatchedIdentity ? "COSMIC_WRITE_INTENT_INVALID" : wizard
            ? CosmicConcurrencyException.Code : "COSMIC_WRITE_FORBIDDEN", result.ErrorCode);
        Assert.False(result.IsDeleted);
        if (!wizard || mismatchedIdentity) Assert.Null(submitted);
        else
        {
            Assert.NotNull(submitted);
            Assert.Equal(actor, submitted.Write.ActorId);
            Assert.Equal(operation, submitted.Write.OperationId);
            Assert.Equal(ProviderType.SQLLiteDBOASIS, submitted.Write.Authority);
            Assert.Equal(revision, Assert.Single(submitted.Write.Conditions).ExpectedRevisionId);
            Assert.Equal(soft ? ConditionalHolonOperation.SoftDelete : ConditionalHolonOperation.HardDelete, submitted.Operation);
        }
        Assert.DoesNotContain(provider.Invocations, call => call.Method.Name == "DeleteHolonAsync");
    }
}
