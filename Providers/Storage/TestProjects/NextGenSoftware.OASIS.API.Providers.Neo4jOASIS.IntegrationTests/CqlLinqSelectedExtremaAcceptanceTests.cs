using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static void VerifySelectedExtrema(ICosmicQueryStorageProvider adapter, IQueryable<Holon> root,
        IQueryable<Holon> multi, List<object> checks)
    {
        if (adapter.QueryProviderType != ProviderType.SQLLiteDBOASIS && adapter.QueryProviderType != ProviderType.MongoDBOASIS) return;
        var before = checks.Count;
        Domain<byte>("extByte", byte.MaxValue);
        Domain<byte?>("extByte", byte.MaxValue);
        Domain<sbyte>("extSByte", sbyte.MinValue);
        Domain<sbyte?>("extSByte", sbyte.MinValue);
        Domain<short>("extShort", short.MinValue);
        Domain<short?>("extShort", short.MinValue);
        Domain<ushort>("extUShort", ushort.MaxValue);
        Domain<ushort?>("extUShort", ushort.MaxValue);
        Domain<int>("int", -7);
        Domain<int?>("int", -7);
        Domain<uint>("extUInt", uint.MaxValue);
        Domain<uint?>("extUInt", uint.MaxValue);
        Domain<long>("i64", 9007199254740993L);
        Domain<long?>("i64", 9007199254740993L);
        Domain<ulong>("u64", ulong.MaxValue);
        Domain<ulong?>("u64", ulong.MaxValue);
        Domain<decimal>("decimal", 1.0000000000000000000000000001m);
        Domain<decimal?>("decimal", 1.0000000000000000000000000001m);
        Domain<float>("float", 1f + 1.1920929e-7f);
        Domain<float?>("float", 1f + 1.1920929e-7f);
        Domain<double>("double", 1d + 2.220446049250313e-16d);
        Domain<double?>("double", 1d + 2.220446049250313e-16d);
        Domain<bool>("bool", true);
        Domain<bool?>("bool", true);
        Domain<Guid>("guid", root.Select(row => row.CreatedByAvatarId).First());
        Domain<Guid?>("guid", root.Select(row => row.CreatedByAvatarId).First());
        Domain<DateTime>("utc", new DateTime(2026,10,27,0,0,0,DateTimeKind.Utc).AddTicks(7));
        Domain<DateTime?>("utc", new DateTime(2026,10,27,0,0,0,DateTimeKind.Utc).AddTicks(7));
        Domain<HolonType>("enum", HolonType.Quest);
        Domain<HolonType?>("enum", HolonType.Quest);
        Domain<ProviderType>("extProvider", ProviderType.SQLLiteDBOASIS);
        Domain<ProviderType?>("extProvider", ProviderType.SQLLiteDBOASIS);
        Domain<string>("text", "Café 😀");

        Assert.AreEqual(-long.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<long>(row, "checked")).Take(3).Min());
        Passed("multi exact Int64 Min");
        Assert.AreEqual(long.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<long>(row, "checked")).Take(3).Max());
        Passed("multi exact Int64 Max");
        Assert.AreEqual(0UL, multi.Select(row => CosmicQueryMetadata.Value<ulong>(row, "wideU64")).Take(3).Min());
        Passed("multi exact UInt64 Min");
        Assert.AreEqual(ulong.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<ulong>(row, "wideU64")).Take(3).Max());
        Passed("multi exact UInt64 Max");
        Assert.AreEqual(-decimal.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<decimal>(row, "wideDecimal")).Take(3).Min());
        Passed("multi exact decimal Min");
        Assert.AreEqual(decimal.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<decimal>(row, "wideDecimal")).Take(3).Max());
        Passed("multi exact decimal Max");
        Assert.AreEqual(-double.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<double>(row, "wideDouble")).Take(3).Min());
        Passed("multi exact Double Min");
        Assert.AreEqual(double.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<double>(row, "wideDouble")).Take(3).Max());
        Passed("multi exact Double Max");
        Assert.AreEqual(-float.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<float>(row, "wideFloat")).Take(3).Min());
        Passed("multi exact Single Min");
        Assert.AreEqual(float.MaxValue, multi.Select(row => CosmicQueryMetadata.Value<float>(row, "wideFloat")).Take(3).Max());
        Passed("multi exact Single Max");
        Assert.AreEqual(1, multi.Select(row => row.Version).Take(3).Min());
        Passed("multi Int32 Min");
        Assert.IsTrue(multi.Select(row => row.Version).Take(3).Max() >= 1);
        Passed("multi Int32 Max");

        StagedWideDto.Constructions = 0;
        var records = multi.Select(row => new StagedWideDto
        {
            Label = row.Name,
            Amount = CosmicQueryMetadata.Value<decimal>(row, "wideDecimal")
        }).OrderBy(row => row.Label);
        Assert.AreEqual(-decimal.MaxValue, records.Min(row => row.Amount));
        Assert.AreEqual(0, StagedWideDto.Constructions);
        Passed("record Min constructs no DTO");
        Assert.AreEqual(decimal.MaxValue, records.Max(row => row.Amount));
        Assert.AreEqual(0, StagedWideDto.Constructions);
        Passed("record Max constructs no DTO");
        Assert.AreEqual(decimal.MaxValue, records.Skip(1).Take(1).Min(row => row.Amount));
        Passed("record Min follows selected window");
        Assert.AreEqual(-decimal.MaxValue, records.Take(1).Max(row => row.Amount));
        Passed("record Max follows selected window");
        var nullable = multi.Select(row => CosmicQueryMetadata.Value<long?>(row, "i64")).Take(3);
        Assert.AreEqual((long?)9007199254740993L, nullable.Min());
        Passed("mixed missing and present selected Min");
        Assert.AreEqual((long?)9007199254740993L, nullable.Max());
        Passed("mixed missing and present selected Max");
        Assert.AreEqual(218, checks.Count - before);

        void Domain<T>(string key, T expected)
        {
            var values = root.Select(row => CosmicQueryMetadata.Value<T>(row, key)).Take(1);
            Assert.AreEqual(expected, values.Min());
            Passed(typeof(T) + " exact selected Min");
            Assert.AreEqual(expected, values.Max());
            Passed(typeof(T) + " exact selected Max");
            if (!typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                Assert.IsNull(values.Take(0).Min());
                Assert.IsNull(values.Take(0).Max());
                var allNull = root.Select(row => CosmicQueryMetadata.Value<T>(row, "nullable")).Take(1);
                Assert.IsNull(allNull.Min());
                Assert.IsNull(allNull.Max());
                Passed(typeof(T) + " all-null selected Min");
                Passed(typeof(T) + " all-null selected Max");
                var missing = root.Select(row => CosmicQueryMetadata.Value<T>(row, "missing-extrema")).Take(1);
                Assert.IsNull(missing.Min());
                Assert.IsNull(missing.Max());
                Passed(typeof(T) + " all-missing selected Min");
                Passed(typeof(T) + " all-missing selected Max");
            }
            else
            {
                Assert.AreEqual("CQL_EMPTY_SEQUENCE", Assert.ThrowsException<CosmicLinqException>(() => values.Take(0).Min()).Code);
                Assert.AreEqual("CQL_EMPTY_SEQUENCE", Assert.ThrowsException<CosmicLinqException>(() => values.Take(0).Max()).Code);
            }
            Passed(typeof(T) + " empty selected Min");
            Passed(typeof(T) + " empty selected Max");
        }
        void Passed(string label) => checks.Add(new { provider = adapter.QueryProviderType.ToString(), label });
    }
}
