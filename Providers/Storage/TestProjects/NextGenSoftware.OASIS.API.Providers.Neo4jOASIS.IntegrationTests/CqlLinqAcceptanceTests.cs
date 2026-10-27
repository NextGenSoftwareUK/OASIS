using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static HolonManager NativeManager(ICosmicQueryStorageProvider adapter)
    {
        var dna = new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = HyperDriveModes.V2,
                StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
            }
        };
        var registry = new ProviderManager(null, dna)
        { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        return new HolonManager((IOASISStorageProvider)adapter, dna, registry);
    }

    private static async Task VerifyLinqRows(ICosmicQueryStorageProvider adapter, Guid rootId, List<object> checks)
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.Id == rootId).Result;
        var captured = CosmicLinq.Create(NativeManager(adapter), adapter, source, maximumResults: 10);
        Assert.IsFalse(captured.IsError, captured.Message);
        var root = captured.Result;
        var name = "protected source root";
        var query = root.Where(row => row.Name == name)
            .OrderByDescending(row => row.Name).ThenBy(row => row.Name).Skip(0).Take(1);

        var read = await query.ToCosmicListAsync();
        Assert.IsFalse(read.IsError, read.Message + read.Exception);
        Assert.IsTrue(read.IsLoaded); Assert.AreEqual(rootId, read.Result.Single().Id);
        Assert.IsFalse(HolonChangeTracking.HasChanges(read.Result.Single()));
        Passed("deferred native rows");
        name = "changed before next execution";
        var rebound = await query.ToCosmicListAsync();
        Assert.IsFalse(rebound.IsError, rebound.Message); Assert.AreEqual(0, rebound.Result.Count);
        Passed("standard closure binding per execution");
        name = "protected source root";

        Assert.AreEqual(rootId, query.ToList().Single().Id);
        Passed("synchronous standard enumeration");
        var empty = await root.Where(row => row.Name == "absent").ToCosmicListAsync();
        Assert.IsFalse(empty.IsError, empty.Message); Assert.AreEqual(0, empty.Result.Count);
        Passed("native false predicate");
        empty = await root.Skip(-2).Take(-3).ToCosmicListAsync();
        Assert.IsFalse(empty.IsError, empty.Message); Assert.AreEqual(0, empty.Result.Count);
        Passed("standard negative paging semantics");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var cancelled = await query.ToCosmicListAsync(cancellation.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode); Assert.IsNull(cancelled.Result);
        Passed("explicit async cancellation");
        var unsupported = Assert.ThrowsException<CosmicLinqException>(() => query.Select(row => row.Name).Count());
        Assert.AreEqual("CQL_EXPRESSION_UNSUPPORTED", unsupported.Code);
        Passed("pending selected numeric terminal fails explicitly");
        Assert.AreEqual(1, root.Count()); Passed("native Count");
        Assert.AreEqual(1L, root.LongCount()); Passed("native LongCount");
        Assert.IsTrue(root.Any()); Passed("native Any");
        Assert.IsTrue(root.All(row => row.Name == name)); Passed("native All true");
        Assert.IsFalse(root.All(row => row.Name == "absent")); Passed("native All false");
        Assert.IsTrue(root.Take(0).All(row => false)); Passed("native All empty window");
        Assert.IsFalse(root.Take(0).Any()); Passed("native Any empty window");
        Assert.AreEqual(0, root.Where(row => row.Name == "absent").Count()); Passed("native Count empty source");
        Assert.AreEqual(1, root.Count(row => row.Name == name)); Passed("native Count predicate");
        Assert.AreEqual("CQL_COMPOSITION_UNSUPPORTED", Assert.ThrowsException<CosmicLinqException>(
            () => root.Take(1).Count(row => row.Name == name)).Code); Passed("post-window predicate remains explicit unsupported");
        Assert.IsTrue(root.All(row => row.Children.Any(child => child.Name == "visible match")));
        Passed("native All child scope");

        var names = root.Select(row => row.Name);
        var nativeNames = await names.ToCosmicListAsync();
        Assert.IsFalse(nativeNames.IsError, nativeNames.Message + nativeNames.Exception);
        Assert.AreEqual("protected source root", nativeNames.Result.Single()); Passed("native scalar Select");
        Assert.AreEqual("protected source root", names.ToList().Single()); Passed("synchronous selected enumeration");
        var records = root.Select(row => new { Label = row.Name, row.Version });
        var nativeRecords = await records.ToCosmicListAsync();
        Assert.IsFalse(nativeRecords.IsError, nativeRecords.Message + nativeRecords.Exception);
        Assert.AreEqual("protected source root", nativeRecords.Result.Single().Label); Passed("native flat record Select");
        var label = "protected source root";
        var composed = records.Where(value => value.Label == label)
            .OrderByDescending(value => value.Label).ThenBy(value => value.Version).Skip(0).Take(1);
        var selected = await composed.ToCosmicListAsync();
        Assert.IsFalse(selected.IsError, selected.Message + selected.Exception);
        Assert.AreEqual("protected source root", selected.Result.Single().Label); Passed("native selected composition");
        label = "absent";
        selected = await composed.ToCosmicListAsync();
        Assert.IsFalse(selected.IsError, selected.Message + selected.Exception);
        Assert.AreEqual(0, selected.Result.Count); Passed("selected predicate rebinding");
        label = "protected source root";
        Assert.IsTrue(composed.All(value => value.Label == label)); Passed("standard selected All true");
        Assert.IsFalse(composed.All(value => value.Version < 0)); Passed("standard selected All false");
        Assert.IsTrue(composed.Take(0).All(value => false)); Passed("standard selected All empty window");
        var emptySelected = await records.Skip(-2).Take(-3).ToCosmicListAsync();
        Assert.IsFalse(emptySelected.IsError, emptySelected.Message + emptySelected.Exception);
        Assert.AreEqual(0, emptySelected.Result.Count); Passed("selected negative window");
        var selectedCancelled = await composed.ToCosmicListAsync(cancellation.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", selectedCancelled.ErrorCode); Passed("selected cancellation");

        var ordered = root.OrderBy(row => row.Name);
        var elementCases = new (string Label, Func<Holon> Read)[]
        {
            ("First", () => root.First()), ("FirstOrDefault", () => root.FirstOrDefault()!),
            ("Single", () => root.Single()), ("SingleOrDefault", () => root.SingleOrDefault()!),
            ("Last", () => ordered.Last()), ("LastOrDefault", () => ordered.LastOrDefault()!),
            ("ElementAt Int32", () => root.ElementAt(0)), ("ElementAt Index", () => ordered.ElementAt(^1))
        };
        foreach(var item in elementCases)
        {
            var row = item.Read(); Assert.AreEqual(rootId,row.Id); Assert.IsFalse(HolonChangeTracking.HasChanges(row));
            Passed("native standard " + item.Label);
        }
        Assert.IsNull(root.ElementAtOrDefault(100)); Passed("element out of range default");
        Assert.IsNull(root.Take(0).FirstOrDefault()); Passed("FirstOrDefault empty source");
        var supplied = new Holon { Name="caller default" };
        Assert.AreSame(supplied,ordered.Take(0).LastOrDefault(supplied)); Passed("LastOrDefault supplied default");
        Assert.AreEqual("CQL_SEQUENCE_EMPTY",Assert.ThrowsException<CosmicLinqException>(
            () => root.Where(row=>row.Name=="absent").First()).Code); Passed("First empty error");
        var actor = read.Result.Single().CreatedByAvatarId;
        var multiSource = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row=>row.CreatedByAvatarId==actor).Result;
        var multi = CosmicLinq.Create(NativeManager(adapter),adapter,multiSource).Result;
        Assert.AreEqual("CQL_SEQUENCE_MULTIPLE",Assert.ThrowsException<CosmicLinqException>(()=>multi.Single()).Code);
        Passed("Single multiple error");
        Assert.AreEqual("CQL_SEQUENCE_MULTIPLE",Assert.ThrowsException<CosmicLinqException>(()=>multi.SingleOrDefault(supplied)).Code);
        Passed("SingleOrDefault default cannot mask multiple");
        Assert.AreEqual(rootId,root.First(row=>row.Name==name).Id); Passed("First predicate before window");
        Assert.AreEqual("CQL_COMPOSITION_UNSUPPORTED",Assert.ThrowsException<CosmicLinqException>(
            ()=>root.Take(1).First(row=>row.Name==name)).Code); Passed("First predicate after window explicit unsupported");
        Assert.AreSame(supplied,root.Where(row=>row.Name=="absent").FirstOrDefault(supplied)); Passed("FirstOrDefault supplied default");
        Assert.IsNull(root.ElementAtOrDefault(-1)); Passed("negative ElementAtOrDefault");

        var version = read.Result.Single().Version;
        Assert.AreEqual(version,root.Sum(row=>row.Version));Passed("native selector Sum");
        Assert.AreEqual(version,root.Min(row=>row.Version));Passed("native selector Min");
        Assert.AreEqual("protected source root",root.Max(row=>row.Name));Passed("native selector Max");
        Assert.AreEqual(9007199254740993L,root.Sum(row=>CosmicQueryMetadata.Value<long>(row,"i64")));Passed("exact Int64 Sum");
        const decimal exactDecimal=1.0000000000000000000000000001m;
        Assert.AreEqual(exactDecimal,root.Sum(row=>CosmicQueryMetadata.Value<decimal>(row,"decimal")));Passed("exact decimal Sum");
        Assert.AreEqual(exactDecimal,root.Average(row=>CosmicQueryMetadata.Value<decimal>(row,"decimal")));Passed("native decimal Average");
        Assert.AreEqual(1d+2.220446049250313e-16d,root.Average(row=>CosmicQueryMetadata.Value<double>(row,"double")));Passed("native Double Average");
        Assert.AreEqual(1f+1.1920929e-7f,root.Average(row=>CosmicQueryMetadata.Value<float>(row,"float")));Passed("native Single Average");
        Assert.AreEqual((int?)0,root.Sum(row=>CosmicQueryMetadata.Value<int?>(row,"nullable")));Passed("all-null nullable Sum");
        Assert.AreEqual(0,root.Take(0).Sum(row=>row.Version));Passed("empty source Sum");
        Assert.AreEqual("CQL_EMPTY_SEQUENCE",Assert.ThrowsException<CosmicLinqException>(
            ()=>root.Take(0).Average(row=>CosmicQueryMetadata.Value<decimal>(row,"decimal"))).Code);Passed("required Average empty error");
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",Assert.ThrowsException<CosmicLinqException>(
            ()=>root.Average(row=>CosmicQueryMetadata.Value<long>(row,"i64"))).Code);Passed("Int64 checked-prefix Average remains explicit gap");
        Assert.AreEqual(version,root.Select(row=>row.Version).Sum());Passed("plain selected Sum");
        Assert.AreEqual("protected source root",root.Select(row=>row.Name).Min());Passed("plain selected Min");
        Assert.AreEqual(exactDecimal,root.Select(row=>CosmicQueryMetadata.Value<decimal>(row,"decimal")).Average());Passed("plain selected Average");
        Assert.AreEqual("CQL_EXPRESSION_UNSUPPORTED",Assert.ThrowsException<CosmicLinqException>(
            ()=>root.Select(row=>row.Version).Where(value=>value>0).Sum()).Code);Passed("staged numeric reduction remains explicit gap");

        Assert.AreEqual((double)version,root.Average(row=>row.Version));Passed("native Int32 Average double output");
        Assert.AreEqual((double)version,root.Select(row=>row.Version).Average());Passed("plain selected Int32 Average");
        Assert.IsNull(root.Average(row=>CosmicQueryMetadata.Value<int?>(row,"nullable")));Passed("nullable Int32 Average all-null");
        Assert.AreEqual(-7d,root.Average(row=>CosmicQueryMetadata.Value<int>(row,"int")));Passed("negative Int32 Average");
        Assert.AreEqual("CQL_EMPTY_SEQUENCE",Assert.ThrowsException<CosmicLinqException>(()=>root.Take(0).Average(row=>row.Version)).Code);
        Passed("required Int32 Average empty error");
        Assert.IsNull(root.Take(0).Average(row=>CosmicQueryMetadata.Value<int?>(row,"nullable")));Passed("nullable Int32 Average empty");

        Assert.AreEqual((double?)-7d,root.Average(row=>CosmicQueryMetadata.Value<int?>(row,"int")));
        Passed("participating nullable Int32 Average");
        Assert.AreEqual((double?)-7d,root.Select(row=>CosmicQueryMetadata.Value<int?>(row,"int")).Average());
        Passed("plain selected nullable Int32 Average");

        void Passed(string label) => checks.Add(new { provider = adapter.QueryProviderType.ToString(), label });
    }
}
