using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    public sealed class StagedWideDto
    {
        public static int Constructions;
        public StagedWideDto() => Constructions++;
        public string Label { get; set; } = "";
        public decimal Amount { get; set; }
        public double Binary { get; set; }
        public float Single { get; set; }
        public ulong Counter { get; set; }
    }

    private static async Task VerifyStagedWide(ICosmicQueryStorageProvider adapter, Guid rootId, Guid actor,
        IQueryable<Holon> root, IQueryable<Holon> multi, List<object> checks)
    {
        if (adapter.QueryProviderType != ProviderType.SQLLiteDBOASIS && adapter.QueryProviderType != ProviderType.MongoDBOASIS) return;
        const decimal exact = 1.0000000000000000000000000001m;
        const double exactDouble = 1d + 2.220446049250313e-16d;
        const float exactSingle = 1f + 1.1920929e-7f;

        Assert.AreEqual(exact, root.Select(row => CosmicQueryMetadata.Value<decimal>(row, "decimal")).Take(1).Sum());
        Passed("staged exact decimal Sum");
        Assert.AreEqual(exact, root.Select(row => CosmicQueryMetadata.Value<decimal>(row, "decimal")).Take(1).Average());
        Passed("staged exact decimal Average");
        Assert.AreEqual((decimal?)exact, root.Select(row => CosmicQueryMetadata.Value<decimal?>(row, "decimal")).Take(1).Sum());
        Passed("staged participating nullable decimal Sum");
        Assert.IsNull(root.Select(row => CosmicQueryMetadata.Value<decimal?>(row, "nullable")).Take(1).Average());
        Passed("staged all-null nullable decimal mean");
        Assert.AreEqual(0m, root.Select(row => CosmicQueryMetadata.Value<decimal>(row, "decimal")).Take(0).Sum());
        Passed("staged empty decimal Sum");
        Assert.AreEqual("CQL_EMPTY_SEQUENCE", Assert.ThrowsException<CosmicLinqException>(() =>
            root.Select(row => CosmicQueryMetadata.Value<decimal>(row, "decimal")).Take(0).Average()).Code);
        Passed("staged required decimal mean empty error");
        Assert.AreEqual(0.0000000000000000000000000001m,
            multi.Select(row => CosmicQueryMetadata.Value<decimal>(row, "wideDecimal")).Take(3).Sum());
        Passed("staged decimal cancellation retains smallest fraction");
        Assert.AreEqual("CQL_SCALAR_OVERFLOW", Assert.ThrowsException<CosmicLinqException>(() =>
            multi.Select(row => CosmicQueryMetadata.Value<decimal>(row, "decimalMax")).Take(3).Sum()).Code);
        Passed("staged decimal Sum overflow");
        Assert.AreEqual(decimal.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<decimal>(row, "decimalMax")).Take(3).Average());
        Passed("staged overflowing decimal total has representable mean");

        Assert.AreEqual(exactDouble, root.Select(row => CosmicQueryMetadata.Value<double>(row, "double")).Take(1).Sum());
        Passed("staged exact Double Sum");
        Assert.AreEqual(exactDouble, root.Select(row => CosmicQueryMetadata.Value<double>(row, "double")).Take(1).Average());
        Passed("staged exact Double mean");
        Assert.AreEqual((double?)exactDouble, root.Select(row => CosmicQueryMetadata.Value<double?>(row, "double")).Take(1).Sum());
        Passed("staged participating nullable Double Sum");
        Assert.IsNull(root.Select(row => CosmicQueryMetadata.Value<double?>(row, "nullable")).Take(1).Average());
        Passed("staged all-null nullable Double mean");
        Assert.AreEqual(0d, root.Select(row => CosmicQueryMetadata.Value<double>(row, "double")).Take(0).Sum());
        Passed("staged empty Double Sum");
        Assert.AreEqual("CQL_EMPTY_SEQUENCE", Assert.ThrowsException<CosmicLinqException>(() =>
            root.Select(row => CosmicQueryMetadata.Value<double>(row, "double")).Take(0).Average()).Code);
        Passed("staged required Double mean empty error");
        Assert.AreEqual(double.Epsilon, multi.Select(row => CosmicQueryMetadata.Value<double>(row, "wideDouble")).Take(3).Sum());
        Passed("staged Double cancellation preserves subnormal");
        Assert.AreEqual("CQL_SCALAR_OVERFLOW", Assert.ThrowsException<CosmicLinqException>(() =>
            multi.Select(row => CosmicQueryMetadata.Value<double>(row, "doubleMax")).Take(3).Sum()).Code);
        Passed("staged Double Sum overflow");
        Assert.AreEqual(double.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<double>(row, "doubleMax")).Take(3).Average());
        Passed("staged overflowing Double total has finite mean");

        Assert.AreEqual(exactSingle, root.Select(row => CosmicQueryMetadata.Value<float>(row, "float")).Take(1).Sum());
        Passed("staged exact Single Sum");
        Assert.AreEqual(exactSingle, root.Select(row => CosmicQueryMetadata.Value<float>(row, "float")).Take(1).Average());
        Passed("staged exact Single mean");
        Assert.AreEqual((float?)exactSingle, root.Select(row => CosmicQueryMetadata.Value<float?>(row, "float")).Take(1).Sum());
        Passed("staged participating nullable Single Sum");
        Assert.IsNull(root.Select(row => CosmicQueryMetadata.Value<float?>(row, "nullable")).Take(1).Average());
        Passed("staged all-null nullable Single mean");
        Assert.AreEqual(float.Epsilon, multi.Select(row => CosmicQueryMetadata.Value<float>(row, "wideFloat")).Take(3).Sum());
        Passed("staged Single cancellation preserves subnormal");
        Assert.AreEqual("CQL_SCALAR_OVERFLOW", Assert.ThrowsException<CosmicLinqException>(() =>
            multi.Select(row => CosmicQueryMetadata.Value<float>(row, "floatMax")).Take(3).Sum()).Code);
        Passed("staged Single Sum overflow");
        Assert.AreEqual(float.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<float>(row, "floatMax")).Take(3).Average());
        Passed("staged overflowing Single total has finite mean");

        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result).Where(row => row.Id == rootId).Result;
        var unsigned = source.Select(row => CosmicQueryMetadata.Value<ulong>(row, "u64")).Take(1).Result;
        var unsignedSum = await Direct<ulong, ulong>(unsigned, CosmicScalarOperation.Sum);
        Assert.IsFalse(unsignedSum.IsError, unsignedSum.Message + unsignedSum.Exception);
        Assert.AreEqual(ulong.MaxValue, unsignedSum.Result.Providers.Single().Value);
        Passed("direct staged exact UInt64 Sum");
        var unsignedMean = await Direct<ulong, decimal>(unsigned, CosmicScalarOperation.Average);
        Assert.IsFalse(unsignedMean.IsError, unsignedMean.Message + unsignedMean.Exception);
        Assert.AreEqual((decimal)ulong.MaxValue, unsignedMean.Result.Providers.Single().Value);
        Passed("direct staged exact UInt64 mean");
        var optionalUnsigned = source.Select(row => CosmicQueryMetadata.Value<ulong?>(row, "u64")).Take(1).Result;
        var optionalSum = await Direct<ulong?, ulong?>(optionalUnsigned, CosmicScalarOperation.Sum);
        Assert.IsFalse(optionalSum.IsError, optionalSum.Message + optionalSum.Exception);
        Assert.AreEqual((ulong?)ulong.MaxValue, optionalSum.Result.Providers.Single().Value);
        Passed("direct staged participating nullable UInt64 Sum");
        var nullableMean = await Direct<ulong?, decimal?>(source.Select(row => CosmicQueryMetadata.Value<ulong?>(row, "nullable")).Take(1).Result,
            CosmicScalarOperation.Average);
        Assert.IsFalse(nullableMean.IsError, nullableMean.Message + nullableMean.Exception);
        Assert.IsNull(nullableMean.Result.Providers.Single().Value);
        Passed("direct staged all-null nullable UInt64 mean");
        var emptySum = await Direct<ulong, ulong>(source.Select(row => CosmicQueryMetadata.Value<ulong>(row, "u64")).Take(0).Result,
            CosmicScalarOperation.Sum);
        Assert.IsFalse(emptySum.IsError, emptySum.Message + emptySum.Exception);
        Assert.AreEqual(0UL, emptySum.Result.Providers.Single().Value);
        Passed("direct staged empty UInt64 Sum");
        var emptyMean = await Direct<ulong, decimal>(source.Select(row => CosmicQueryMetadata.Value<ulong>(row, "u64")).Take(0).Result,
            CosmicScalarOperation.Average);
        Assert.AreEqual("CQL_EMPTY_SEQUENCE", emptyMean.ErrorCode);
        Passed("direct staged required UInt64 mean empty error");
        var actorSource = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.CreatedByAvatarId == actor).Result;
        var unsignedPage = actorSource.Select(row => CosmicQueryMetadata.Value<ulong>(row, "wideU64")).Take(3).Result;
        Assert.AreEqual("CQL_SCALAR_OVERFLOW", (await Direct<ulong, ulong>(unsignedPage, CosmicScalarOperation.Sum)).ErrorCode);
        Passed("direct staged UInt64 Sum overflow");
        var wideMean = await Direct<ulong, decimal>(unsignedPage, CosmicScalarOperation.Average);
        Assert.IsFalse(wideMean.IsError, wideMean.Message + wideMean.Exception);
        Assert.AreEqual(((decimal)ulong.MaxValue + 1m) / 3m, wideMean.Result.Providers.Single().Value);
        Passed("direct staged UInt64 wide total mean");

        StagedWideDto.Constructions = 0;
        var records = multi.Select(row => new StagedWideDto
        {
            Label = row.Name, Amount = CosmicQueryMetadata.Value<decimal>(row, "wideDecimal"),
            Binary = CosmicQueryMetadata.Value<double>(row, "wideDouble"), Single = CosmicQueryMetadata.Value<float>(row, "wideFloat"),
            Counter = CosmicQueryMetadata.Value<ulong>(row, "wideU64")
        }).OrderBy(row => row.Label).Take(3);
        Assert.AreEqual(0.0000000000000000000000000001m, records.Sum(row => row.Amount));
        Assert.AreEqual(0, StagedWideDto.Constructions);
        Passed("mixed-width selected decimal record Sum constructs no DTO");
        Assert.AreEqual(double.Epsilon, records.Sum(row => row.Binary));
        Assert.AreEqual(0, StagedWideDto.Constructions);
        Passed("mixed-width selected Double record Sum constructs no DTO");
        Assert.AreEqual(float.Epsilon, records.Sum(row => row.Single));
        Assert.AreEqual(0, StagedWideDto.Constructions);
        Passed("mixed-width selected Single record Sum constructs no DTO");
        Assert.AreEqual(decimal.MaxValue, records.Skip(1).Take(1).Average(row => row.Amount));
        Assert.AreEqual(0, StagedWideDto.Constructions);
        Passed("mixed-width selected window precedes decimal record mean");

        async Task<OASISResult<CosmicScalarExecution<TValue>>> Direct<TSelected, TValue>(
            CosmicProjectionQuery<Holon, TSelected> query, CosmicScalarOperation operation)
        {
            var native = (ICosmicQueryProjectionCompositionStorageProvider)adapter;
            var plan = CosmicProjectedNumericPlanner.Plan<Holon, TSelected, TValue>(query, 0, operation, adapter.QueryProviderType,
                new[] { new CosmicProjectionCompositionTarget(adapter.QueryProviderType, adapter.QueryCapabilities,
                    native.ProjectionCapabilities, native.CompositionCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            return await NativeManager(adapter).ExecuteProjectedNumericQueryAsync(plan.Result, new[] { adapter });
        }
        void Passed(string label) => checks.Add(new { provider = adapter.QueryProviderType.ToString(), label });
    }
}
