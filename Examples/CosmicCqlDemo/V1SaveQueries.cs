using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>Save ownership and change summaries on the same native provider used by CQL.</summary>
public static class V1SaveQueries
{
    public sealed class Coffee : Holon
    {
        [CustomOASISProperty] public int Stock { get; set; }
    }

    // Retain these identities with a logical request. They are write intents,
    // not permission to repeat a write after an unknown acknowledgement.
    public sealed record Operations(Guid Create, Guid NoOp, Guid Update, Guid Delete);

    public static async Task<int> RunAsync(HolonManager manager,
        ICosmicQueryStorageProvider adapter, Guid actor, Coffee coffee,
        Operations operations, TextWriter output)
    {
        var provider = adapter.QueryProviderType;
        Ensure(coffee.Id == Guid.Empty, "Start with an unassigned identity.");
        Ensure(manager.PreparationEncryptionPolicy != null,
            "The trusted host must configure preparation protection for the recovery example.");

        // 1. Save plans a generated identity privately. The caller receives its
        // identity and clean baseline only after the native acknowledgement.
        var created = await manager.SaveHolonAsync(coffee, actor,
            new CosmicWriteOptions(operations.Create), saveChildren: false, providerType: provider);
        Success(created);
        Ensure(created.IsSaved && coffee.Id != Guid.Empty && !HolonChangeTracking.HasChanges(coffee),
            "A committed create must publish its generated identity and clean baseline.");
        var originalSummary = Summary(created, operations.Create, coffee.Id, "Created");
        await output.WriteLineAsync($"\nSave 1. Generated identity: {coffee.Id}\n");

        // 2. An unchanged object is a no-op: no physical mutation, new version
        // or committed delta. The integration gate also observes native calls.
        var version = coffee.Version;
        var revision = CosmicConcurrency.GetProviderRevisionId(coffee, provider);
        var unchanged = await manager.SaveHolonAsync(coffee, actor,
            new CosmicWriteOptions(operations.NoOp), saveChildren: false, providerType: provider);
        Success(unchanged);
        Ensure(!unchanged.IsSaved && coffee.Version == version
            && CosmicConcurrency.GetProviderRevisionId(coffee, provider) == revision
            && !(unchanged.MetaData?.ContainsKey(HolonCommittedDelta.ResultMetadataKey) ?? false),
            "A no-op must preserve the accepted revision and return no invented delta.");
        await output.WriteLineAsync("Save 2. Clean Save(): no new version or delta.\n");

        // 3. Pending preview describes LOCAL edits. It is not commit evidence.
        // Both ordinary typed properties and nested metadata participate.
        coffee.Name += " updated";
        coffee.Stock--;
        coffee.MetaData["added"] = "private added value";
        coffee.MetaData["changed"] = "private changed value";
        coffee.MetaData.Remove("removed");
        Ensure(HolonChangeTracking.HasChanges(coffee), "Edits must make the object dirty.");
        var pending = Value(HolonChangeTracking.PreviewValueChanges(coffee));
        Ensure(pending.Any(change => change.Path.EndsWith("/added") && change.Change == HolonValueChange.Added)
            && pending.Any(change => change.Path.EndsWith("/removed") && change.Change == HolonValueChange.Removed),
            "Local preview must distinguish additions and removals.");
        await output.WriteLineAsync("Save 3. Local pending changes:\n");
        foreach (var change in pending)
            await output.WriteLineAsync($"  {change.Change}: {change.Path}");

        // 4. Only the successful acknowledgement establishes a new clean
        // baseline. The returned summary describes the actual committed write.
        var updated = await manager.SaveHolonAsync(coffee, actor,
            new CosmicWriteOptions(operations.Update), saveChildren: false, providerType: provider);
        Success(updated);
        Ensure(updated.IsSaved && coffee.Version == version + 1
            && !HolonChangeTracking.HasChanges(coffee), "Acknowledgement must accept exactly the saved revision.");
        var updateSummary = Summary(updated, operations.Update, coffee.Id, "Updated");
        using (var json = JsonDocument.Parse(updateSummary))
        {
            var changes = json.RootElement.GetProperty("records")[0].GetProperty("changes").EnumerateArray().ToArray();
            foreach (var (path, kind) in new[] { ("/added", "Added"), ("/removed", "Removed"), ("/changed", "Modified") })
                Ensure(changes.Any(change => change.GetProperty("path").GetString()!.EndsWith(path)
                    && change.GetProperty("change").GetString() == kind), "Committed summary missing " + kind + path);
        }
        await output.WriteLineAsync("\nSave 4. Committed delta: Added, Removed and Modified paths; values stay redacted.\n");

        // 5. A fresh typed CQL read must observe what was committed, with its
        // own clean baseline. This also exercises typed-property hydration.
        var fresh = (await Read()).Single();
        Ensure(fresh.Stock == coffee.Stock && fresh.Name == coffee.Name
            && !fresh.MetaData.ContainsKey("removed") && !HolonChangeTracking.HasChanges(fresh),
            "A fresh read must observe the committed state and clean tracking.");
        await output.WriteLineAsync("Save 5. Fresh typed CQL read observes the committed state.\n");

        // 6. Loaded objects obey the same no-op rule as acknowledged objects.
        var loadedNoOp = await manager.SaveHolonAsync(fresh, actor,
            saveChildren: false, providerType: provider);
        Success(loadedNoOp);
        Ensure(!loadedNoOp.IsSaved && fresh.Version == coffee.Version, "Loaded unchanged objects must not write.");
        await output.WriteLineAsync("Save 6. Loaded-object Save(): also a no-op.\n");

        // 7. Reusing a create intent for DIFFERENT contents is an error. Even
        // on rejection, an uncommitted caller must keep its empty identity.
        var rejectedSource = new Coffee { Name = "different create intent", Stock = 3, IsActive = true };
        var rejected = await manager.SaveHolonAsync(rejectedSource, actor,
            new CosmicWriteOptions(operations.Create), saveChildren: false, providerType: provider);
        Ensure(rejected.IsError && !rejected.IsSaved && rejectedSource.Id == Guid.Empty
            && !(rejected.MetaData?.ContainsKey(HolonCommittedDelta.ResultMetadataKey) ?? false),
            "Rejected creates must publish neither a generated identity nor a committed delta.");
        await output.WriteLineAsync("Save 7. Rejected create keeps its caller identity empty.\n");

        // 8. Entity deletion is different from removing a metadata property.
        // The acknowledged delete has its own operation-bound summary.
        var deleted = await manager.DeleteHolonAsync(coffee, actor,
            new CosmicWriteOptions(operations.Delete), false, provider);
        Success(deleted);
        Ensure(deleted.IsDeleted, "Hard delete must be acknowledged.");
        Summary(deleted, operations.Delete, coffee.Id, "HardDeleted");
        Ensure((await Read()).Length == 0, "Hard-deleted data must not reappear in CQL.");
        await output.WriteLineAsync("Save 8. HardDeleted entity; CQL now returns no row.\n");

        // 9. Old operation summaries are durable evidence, independent of the
        // current row. Only trusted host callbacks choose keys/runtime types.
        // This is REDACTED summary recovery, not historical value decryption.
        var historical = await manager.RecoverConditionalHolonWriteOutcomeAsync(
            operations.Create, actor, provider,
            id => id == coffee.Id ? manager.PreparationEncryptionPolicy : null!,
            (name, assembly) => name == typeof(Coffee).FullName && assembly == typeof(Coffee).Assembly.GetName().Name
                ? typeof(Coffee) : name == typeof(Holon).FullName && assembly == typeof(Holon).Assembly.GetName().Name
                    ? typeof(Holon) : null!);
        Success(historical);
        Ensure(historical.Result.ReceiptState == ConditionalHolonReceiptState.Committed
            && historical.Result.CommittedDeltaSummaryJson == originalSummary,
            "An old operation must retain its original summary after updates and deletion.");
        await output.WriteLineAsync("Save 9. Original creation summary still reads after later update/delete.\n");

        // 10. Another actor cannot retrieve the preparation or its old summary.
        var foreign = await manager.ReadHolonWritePreparationAsync(operations.Create, Guid.NewGuid(), provider);
        Ensure(foreign.IsError && foreign.Result == null, "History lookup must remain bound to its authenticated actor.");
        await output.WriteLineAsync("Save 10. Foreign actor history lookup rejected.\n");

        return 10;

        async Task<Coffee[]> Read()
        {
            var id = coffee.Id;
            var query = Value(CosmicQuery<Coffee>.Create(Value(CosmicQueryScope.Named(provider)))
                .WithReadAccess(Value(CosmicQueryReadAccess.OwnerOrPublic(actor)))
                .Where(row => row.Id == id));
            var plan = Value(CosmicQueryPlanner.Plan(query, provider,
                new[] { new CosmicQueryTarget(provider, adapter.QueryCapabilities) }));
            var execution = Value(await manager.ExecuteQueryAsync(plan, new[] { adapter }));
            return execution.Providers.Single().Rows.ToArray();
        }
    }

    private static string Summary<T>(OASISResult<T> result, Guid operation, Guid id, string kind)
    {
        Ensure(result.MetaData != null && result.MetaData.TryGetValue(HolonCommittedDelta.ResultMetadataKey, out _),
            "Acknowledgement must include a committed summary.");
        var text = result.MetaData![HolonCommittedDelta.ResultMetadataKey];
        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Ensure(root.GetProperty("operationId").GetString() == operation.ToString("D"), "Delta operation binding changed.");
        var records = root.GetProperty("records");
        Ensure(records.GetArrayLength() == 1 && records[0].GetProperty("holonId").GetString() == id.ToString("D")
            && records[0].GetProperty("kind").GetString() == kind, "Delta record/classification binding changed.");
        Ensure(!text.Contains("previousJson") && !text.Contains("currentJson") && !text.Contains("private added value"),
            "Result summaries must not expose stored historical values.");
        return text;
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Success<T>(OASISResult<T> result)
    {
        if (result.IsError) throw new InvalidOperationException(result.ErrorCode + ": " + result.Message, result.Exception);
    }

    private static T Value<T>(OASISResult<T> result)
    {
        Success(result);
        return result.Result;
    }
}
