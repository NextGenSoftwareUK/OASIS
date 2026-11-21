using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(false, "actor")][InlineData(true, "actor")]
    [InlineData(false, "authority")][InlineData(true, "authority")]
    [InlineData(false, "unpinned")][InlineData(true, "unpinned")]
    [InlineData(false, "delete")][InlineData(true, "delete")]
    [InlineData(false, "no-condition")][InlineData(true, "no-condition")]
    [InlineData(false, "invalid-mask")][InlineData(true, "invalid-mask")]
    [InlineData(false, "prepared-mask")][InlineData(true, "prepared-mask")]
    [InlineData(false, "conditions")][InlineData(true, "conditions")]
    [InlineData(false, "roots")][InlineData(true, "roots")]
    [InlineData(false, "flags")][InlineData(true, "flags")]
    [InlineData(false, "enrollment")][InlineData(true, "enrollment")]
    [InlineData(false, "valid-unbound")][InlineData(true, "valid-unbound")]
    public async Task DirectSelectedWriteAdmissionPreservesPreparedIntentBeforeIo(bool synchronous, string mode)
    {
        var f = Fixture(true, conditional: true, preparation: true);
        var source = new Holon { Id = Guid.NewGuid(), Description = "selected" };
        var actor = Guid.NewGuid(); var revision = Guid.NewGuid(); var operation = Guid.NewGuid();
        var enrollment = new HolonReplicationEnrollment("selected-source", new[] { "target-a" });
        var write = new ConditionalHolonWrite(operation, actor, ProviderType.SQLLiteDBOASIS,
            new[] { new HolonWriteCondition(source.Id, revision) }, enrollment);
        var selection = new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = new[] { "/Description" } };
        var preparation = new HolonConditionalPreparation(new[] { source }, write, false, false, 0,
            _ => MetadataPolicy(new MetadataResolver()), ConditionalHolonOperation.Save, selection).Seal();
        var requestedWrite = mode == "conditions" || mode == "enrollment"
            ? new ConditionalHolonWrite(operation, actor, write.Authority,
                new[] { new HolonWriteCondition(source.Id, mode == "conditions" ? Guid.NewGuid() : revision) },
                mode == "enrollment" ? new HolonReplicationEnrollment("selected-source", new[] { "target-b" }) : enrollment)
            : write;
        if (mode == "prepared-mask") selection[source.Id] = new[] { "/Name" };
        if (mode == "invalid-mask") selection[source.Id] = new[] { "Description" };
        var request = new StorageOperationRequest
        {
            Operation = mode == "delete" ? "DeleteHolon" : "SaveHolons",
            Payload = mode == "roots" ? new[] { new Holon { Id = Guid.NewGuid() } } : new[] { source },
            AvatarId = mode == "actor" ? Guid.NewGuid() : actor,
            PreferredProvider = mode == "authority" ? ProviderType.MongoDBOASIS : write.Authority,
            RequirePreferredProvider = mode != "unpinned", AutoReplicate = false,
            SaveChildren = false, Recursive = mode == "flags", MaxChildDepth = 0,
            ConditionalWrite = mode == "no-condition" ? null! : requestedWrite,
            ConditionalPreparation = preparation, ConditionalPropertySelections = selection
        };
        f.Provider.Invocations.Clear();
        var router = new Managers.OASISHyperDrive.OASISHyperDrive(f.Providers);
        var result = synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request)
            : await router.RouteRequestAsync<IEnumerable<IHolon>>(request);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.Equal(mode == "valid-unbound" ? "COSMIC_PROPERTY_WRITE_UNSUPPORTED" : "COSMIC_PROPERTY_WRITE_INTENT_INVALID", result.ErrorCode);
        Assert.DoesNotContain(f.Provider.Invocations, call => call.Method.Name.Contains("ClaimHolon")
            || call.Method.Name.Contains("ExecuteConditional") || call.Method.Name.Contains("ExecuteReplicating")
            || call.Method.Name.StartsWith("SaveHolon") || call.Method.Name.StartsWith("LoadHolon"));
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task DirectSelectedWriteAdmissionFreezesCallerMask(bool synchronous)
    {
        var f = Fixture(true, conditional: true);
        var source = new Holon { Id = Guid.NewGuid() }; var actor = Guid.NewGuid();
        var paths = new[] { "/Description" };
        var selection = new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = paths };
        var request = new StorageOperationRequest
        {
            Operation = "SaveHolons", Payload = new[] { source }, AvatarId = actor,
            PreferredProvider = ProviderType.SQLLiteDBOASIS, RequirePreferredProvider = true,
            AutoReplicate = false, SaveChildren = false, Recursive = false,
            ConditionalWrite = new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS,
                new[] { new HolonWriteCondition(source.Id, Guid.NewGuid()) }),
            ConditionalPropertySelections = selection
        };
        var router = new Managers.OASISHyperDrive.OASISHyperDrive(f.Providers);
        var result = synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request)
            : await router.RouteRequestAsync<IEnumerable<IHolon>>(request);
        Assert.Equal("COSMIC_PROPERTY_WRITE_UNSUPPORTED", result.ErrorCode);
        paths[0] = "/Name"; selection[source.Id] = new[] { "/MetaData" };
        Assert.Equal("/Description", Assert.Single(request.ConditionalPropertySelections[source.Id]));
    }
}
