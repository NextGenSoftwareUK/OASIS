using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Configuration;
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

public class HostedConditionalDeleteConcurrencyTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task InFlightDeleteRetainsItsProviderAndIntentWhileAnotherRequestCompletes(bool byKey, bool soft)
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var firstRevision = Guid.NewGuid();
        var secondRevision = Guid.NewGuid();
        var firstOperation = Guid.NewGuid();
        var secondOperation = Guid.NewGuid();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writes = new List<ConditionalHolonWriteRequest>();
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        Mock<IOASISStorageProvider> Provider(ProviderType type, Guid revision)
        {
            var mock = new Mock<IOASISStorageProvider>();
            mock.As<IConditionalHolonStorageProvider>();
            mock.SetupAllProperties();
            mock.Object.ProviderType = new EnumValue<ProviderType>(type);
            mock.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
            mock.Object.IsProviderActivated = true;
            mock.Setup(value => value.ActivateProvider()).Returns(new OASISResult<bool>(true));
            async Task<OASISResult<IHolon>> Read()
            {
                if (type == ProviderType.SQLLiteDBOASIS)
                {
                    entered.TrySetResult();
                    await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
                }
                var record = new Holon { Id = id, CreatedByAvatarId = actor };
                CosmicConcurrency.SetProviderRevisionId(record, type, revision);
                return new OASISResult<IHolon>(record) { IsLoaded = true };
            }
            mock.Setup(value => value.LoadHolonAsync(id, false, true, 0, true, false, 0)).Returns(Read);
            mock.Setup(value => value.LoadHolonAsync("native-key", false, true, 0, true, false, 0)).Returns(Read);
            mock.As<IConditionalHolonStorageProvider>()
                .Setup(value => value.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
                .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => writes.Add(request))
                .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true,
                    ErrorCode = CosmicConcurrencyException.Code, Message = "Controlled conditional conflict" });
            return mock;
        }
        var firstProvider = Provider(ProviderType.SQLLiteDBOASIS, firstRevision);
        var secondProvider = Provider(ProviderType.MongoDBOASIS, secondRevision);
        var registry = new ProviderManager(null, dna);
        registry.RegisterProvider(firstProvider.Object);
        registry.RegisterProvider(secondProvider.Object);
        Assert.False(registry.SetAndActivateCurrentStorageProvider(firstProvider.Object).IsError);
        var manager = new HolonManager(null, dna, registry);
        DefaultHttpContext Context()
        {
            var context = new DefaultHttpContext { RequestServices = services };
            context.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = actor };
            return context;
        }
        DeleteHolonRequest Request(ProviderType type, Guid operation, Guid revision) => new()
        {
            Id = id, ProviderKey = byKey ? "native-key" : null!, SoftDelete = soft,
            ProviderType = type.ToString(), OperationId = operation,
            ExpectedRevisions = new Dictionary<Guid, Guid?> { [id] = revision }
        };
        var firstRequest = Request(ProviderType.SQLLiteDBOASIS, firstOperation, firstRevision);
        var pending = HostedHolonWriteEndpoint.DeleteAsync(Context(), HyperDriveModes.V2, firstRequest, () => manager);
        OASISResult<IHolon>? firstResult = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(registry.PublishRoutingOverrides(new ProviderRoutingOverrides(), ProviderType.MongoDBOASIS).IsError);
            // Once execution begins, a mutable transport DTO must not replace
            // the captured identity, authority, operation or revision.
            firstRequest.Id = Guid.NewGuid();
            firstRequest.ProviderType = "MongoDBOASIS";
            firstRequest.ProviderKey = "changed-key";
            firstRequest.SoftDelete = !soft;
            firstRequest.OperationId = Guid.NewGuid();
            firstRequest.ExpectedRevisions![id] = Guid.NewGuid();
            var second = await HostedHolonWriteEndpoint.DeleteAsync(Context(), HyperDriveModes.V2,
                Request(ProviderType.MongoDBOASIS, secondOperation, secondRevision), () => manager);
            Assert.Equal(CosmicConcurrencyException.Code, second.ErrorCode);
            Assert.False(pending.IsCompleted, "The first authority read remains paused while the other request finishes.");
        }
        finally
        {
            release.TrySetResult();
            firstResult = await pending.WaitAsync(TimeSpan.FromSeconds(10));
        }
        Assert.Equal(CosmicConcurrencyException.Code, firstResult.ErrorCode);
        Assert.Equal(2, writes.Count);
        foreach (var write in writes)
        {
            var first = write.Write.Authority == ProviderType.SQLLiteDBOASIS;
            Assert.Equal(id, Assert.Single(write.Roots).Id);
            Assert.Equal(actor, write.Write.ActorId);
            Assert.Equal(first ? firstOperation : secondOperation, write.Write.OperationId);
            Assert.Equal(first ? firstRevision : secondRevision, Assert.Single(write.Write.Conditions).ExpectedRevisionId);
            Assert.Equal(soft ? ConditionalHolonOperation.SoftDelete : ConditionalHolonOperation.HardDelete, write.Operation);
        }
        Assert.Single(writes.Where(write => write.Write.Authority == ProviderType.SQLLiteDBOASIS));
        Assert.Single(writes.Where(write => write.Write.Authority == ProviderType.MongoDBOASIS));
    }
}
