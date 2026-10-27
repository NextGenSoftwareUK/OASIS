using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    public sealed class StagedNumericDto
    {
        public static int Constructions;
        public StagedNumericDto() => Constructions++;
        public string Label { get; set; } = "";
        public int Revision { get; set; }
    }

    private static async Task VerifyStagedNumeric(ICosmicQueryStorageProvider adapter, Guid rootId,
        IQueryable<Holon> root, IQueryable<Holon> multi, int version, List<object> checks)
    {
        // Enroll one exact kernel at a time; other providers remain explicit gaps.
        if (adapter.QueryProviderType != ProviderType.SQLLiteDBOASIS) return;

        Assert.AreEqual(version, root.Select(row => row.Version).Take(1).Sum());
        Passed("SQLite staged Int32 Sum");
        Assert.AreEqual((double)version, root.Select(row => row.Version).Take(1).Average());
        Passed("SQLite staged Int32 Average");
        Assert.AreEqual(9007199254740993L, root.Select(row => CosmicQueryMetadata.Value<long>(row, "i64")).Take(1).Sum());
        Passed("SQLite staged exact Int64 Sum");
        Assert.AreEqual((double)9007199254740993L, root.Select(row => CosmicQueryMetadata.Value<long>(row, "i64")).Take(1).Average());
        Passed("SQLite staged Int64 Average");

        StagedNumericDto.Constructions = 0;
        var records = multi.Select(row => new StagedNumericDto { Label = row.Name, Revision = row.Version })
            .OrderBy(value => value.Label).Skip(1).Take(1);
        Assert.AreEqual(version, records.Sum(value => value.Revision));
        Assert.AreEqual(0, StagedNumericDto.Constructions);
        Passed("SQLite staged record Sum constructs no DTO");
        Assert.AreEqual((double)version, records.Average(value => value.Revision));
        Assert.AreEqual(0, StagedNumericDto.Constructions);
        Passed("SQLite staged record Average constructs no DTO");

        Assert.AreEqual((int?)0, root.Select(row => CosmicQueryMetadata.Value<int?>(row, "nullable")).Take(1).Sum());
        Passed("SQLite staged all-null nullable Sum");
        Assert.IsNull(root.Select(row => CosmicQueryMetadata.Value<int?>(row, "nullable")).Take(1).Average());
        Passed("SQLite staged all-null nullable Average");
        Assert.AreEqual("CQL_EMPTY_SEQUENCE", Assert.ThrowsException<CosmicLinqException>(() =>
            root.Select(row => row.Version).Take(0).Average()).Code);
        Passed("SQLite staged required Average empty error");
        Assert.AreEqual(0, root.Select(row => row.Version).Take(0).Sum());
        Passed("SQLite staged empty Sum");
        Assert.IsNull(root.Select(row => CosmicQueryMetadata.Value<long?>(row, "nullable")).Take(0).Average());
        Passed("SQLite staged empty nullable Int64 Average");
        Assert.AreEqual((double?)9007199254740993L,
            root.Select(row => CosmicQueryMetadata.Value<long?>(row, "i64")).Take(1).Average());
        Passed("SQLite staged participating nullable Int64 Average");

        var sourceOrdered = multi.OrderBy(row => CosmicQueryMetadata.Value<int>(row, "checkedOrder"));
        Assert.AreEqual("CQL_SCALAR_OVERFLOW", Assert.ThrowsException<CosmicLinqException>(() =>
            sourceOrdered.Select(row => CosmicQueryMetadata.Value<long>(row, "checked")).Take(3).Average()).Code);
        Passed("SQLite staged Int64 prefix overflow");
        Assert.AreEqual(1d / 3d, multi.Select(row => CosmicQueryMetadata.Value<long>(row, "checked"))
            .OrderBy(value => value).Average());
        Passed("SQLite selected ordering controls Int64 prefixes");
        Assert.AreEqual(new[] { 1L, -long.MaxValue }.Average(),
            sourceOrdered.Select(row => CosmicQueryMetadata.Value<long>(row, "checked")).Skip(1).Average());
        Passed("SQLite selected offset precedes Int64 prefix accumulation");
        Assert.AreEqual("CQL_SCALAR_OVERFLOW", Assert.ThrowsException<CosmicLinqException>(() =>
            sourceOrdered.Select(row => CosmicQueryMetadata.Value<long>(row, "checkedNegative")).Take(3).Average()).Code);
        Passed("SQLite staged negative Int64 prefix overflow");
        Assert.AreEqual(new[] { long.MaxValue, -1L, long.MinValue }.Average(),
            multi.Select(row => CosmicQueryMetadata.Value<long>(row, "checkedNegative")).OrderByDescending(value => value).Average());
        Passed("SQLite selected descending order avoids negative prefix overflow");

        Assert.AreEqual(3, multi.Select(row => CosmicQueryMetadata.Value<int>(row, "checkedOrder")).Where(value => value >= 1).Sum());
        Passed("SQLite selected filter precedes integer Sum");
        Assert.AreEqual(1, multi.Select(row => CosmicQueryMetadata.Value<int>(row, "checkedOrder"))
            .OrderBy(value => value).Take(2).Where(value => value == 1).Sum());
        Passed("SQLite selected filter after page precedes Sum");
        Assert.AreEqual((double)long.MinValue,
            root.Select(row => CosmicQueryMetadata.Value<long>(row, "checkedNegative")).Take(1).Average());
        Passed("SQLite staged Int64 minimum mean");

        // The direct async CQL API retains its exact decimal mean result contract.
        var direct = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.Id == rootId).Select(row => row.Version).Take(1).Result;
        var native = (ICosmicQueryProjectionCompositionStorageProvider)adapter;
        var plan = CosmicProjectedNumericPlanner.Plan<Holon, int, decimal>(direct, 0, CosmicScalarOperation.Average,
            adapter.QueryProviderType, new[] { new CosmicProjectionCompositionTarget(adapter.QueryProviderType,
                adapter.QueryCapabilities, native.ProjectionCapabilities, native.CompositionCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var mean = await NativeManager(adapter).ExecuteProjectedNumericQueryAsync(plan.Result, new[] { adapter });
        Assert.IsFalse(mean.IsError, mean.Message + mean.Exception);
        Assert.AreEqual((decimal)version, mean.Result.Providers.Single().Value);
        Passed("SQLite direct staged decimal integer mean");

        Assert.AreEqual("CQL_SCALAR_OVERFLOW", Assert.ThrowsException<CosmicLinqException>(() =>
            multi.Select(row => CosmicQueryMetadata.Value<int>(row, "intMax")).Take(3).Sum()).Code);
        Passed("SQLite staged Int32 Sum overflow");
        Assert.AreEqual((double)int.MaxValue,
            multi.Select(row => CosmicQueryMetadata.Value<int>(row, "intMax")).Take(3).Average());
        Passed("SQLite staged Int32 wide sum still has valid mean");

        Assert.AreEqual((long?)9007199254740993L,
            root.Select(row => CosmicQueryMetadata.Value<long?>(row, "i64")).Take(1).Sum());
        Passed("SQLite staged participating nullable Int64 Sum");
        Assert.AreEqual((long?)0, root.Select(row => CosmicQueryMetadata.Value<long?>(row, "nullable")).Take(1).Sum());
        Passed("SQLite staged all-null nullable Int64 Sum");
        Assert.AreEqual(-7, root.Select(row => CosmicQueryMetadata.Value<int>(row, "int")).Where(value => value < 0).Sum());
        Passed("SQLite staged negative Int32 Sum");
        Assert.AreEqual(-7d, root.Select(row => CosmicQueryMetadata.Value<int>(row, "int")).Where(value => value < 0).Average());
        Passed("SQLite staged negative Int32 Average");

        void Passed(string label) => checks.Add(new { provider = adapter.QueryProviderType.ToString(), label });
    }
}
