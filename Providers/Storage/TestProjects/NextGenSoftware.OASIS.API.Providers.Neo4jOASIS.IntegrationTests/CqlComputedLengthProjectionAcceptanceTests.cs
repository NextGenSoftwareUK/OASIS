using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    public sealed class NativeLengthProbe
    {
        public static int Constructors;
        public NativeLengthProbe() { Constructors++; }
        public int? Units { get; set; }
    }

    private static async Task VerifyComputedLengthProjection(ICosmicQueryEntityProjectionStorageProvider adapter,
        Guid rootId, List<object> checks)
    {
        var scope = CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        var source = CosmicQuery<Holon>.Create(scope).Where(row => row.Id == rootId).Result;
        var root = CosmicLinq.Create(NativeManager(adapter), adapter, source, maximumResults: 10).Result;
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        void Passed(string label)
        {
            checks.Add(new { provider = adapter.QueryProviderType.ToString(), computedProjection = label, elapsedMs = elapsed.ElapsedMilliseconds });
            elapsed.Restart();
        }

        NativeLengthProbe.Constructors = 0;
        var captured = root.Select(row => new NativeLengthProbe { Units = row.Name.Length });
        Assert.AreEqual(0, NativeLengthProbe.Constructors);
        var read = await captured.ToCosmicListAsync();
        Assert.IsFalse(read.IsError, read.Message + read.Exception);
        Assert.AreEqual(21, read.Result.Single().Units);
        Assert.AreEqual(1, NativeLengthProbe.Constructors);
        Passed("canonical length executes natively before the final DTO constructor");

        Assert.AreEqual(7, root.Select(row => CosmicQueryMetadata.Value<string>(row, "text").Length).Single());
        Assert.AreEqual(5, root.Select(row => CosmicQueryMetadata.Value<string>(row, "lengthProjectionNul").Length).Single());
        Assert.AreEqual(0, root.Select(row => CosmicQueryMetadata.Value<string>(row, "lengthProjectionEmpty").Length).Single());
        Passed("supplementary UTF16, embedded NUL and empty projected strings");

        Assert.IsNull(root.Select(row => (int?)CosmicQueryMetadata.Value<string>(row, "presentNull").Length).Single());
        Assert.IsNull(root.Select(row => (int?)CosmicQueryMetadata.Value<string>(row, "lengthProjectionMissing").Length).Single());
        Assert.IsTrue(root.Select(row => (int?)CosmicQueryMetadata.Value<string>(row, "presentNull").Length).All(value => !value.HasValue));
        Passed("nullable computed values preserve null and missing through native All");

        var siblings = CosmicLinq.Create(NativeManager(adapter), adapter,
            CosmicQuery<Holon>.Create(scope).Where(row => row.Id == rootId || row.ParentHolonId == rootId).Result,
            maximumResults: 10).Result;
        CollectionAssert.AreEqual(new[] { 13, 14, 21 },
            siblings.Select(row => row.Name.Length).Where(value => value > 12).OrderBy(value => value).ToList());
        Assert.AreEqual(14, siblings.Select(row => row.Name.Length).OrderBy(value => value).Skip(1).Take(1).Single());
        Passed("computed alias filtering, numeric ordering and paging use the output domain");

        CollectionAssert.AreEqual(new[] { 21 }, siblings.OrderBy(row => row.Name).Take(3).Reverse().Take(2)
            .Select(row => row.Name.Length).Where(value => value > 14).ToList());
        Passed("entity windows remain before computed selection and later selected stages");

        Assert.AreEqual(48, siblings.Select(row => row.Name.Length).Sum());
        Assert.AreEqual(48, siblings.Sum(row => row.Name.Length));
        Assert.AreEqual(16d, siblings.Average(row => row.Name.Length));
        Assert.AreEqual(13, siblings.Min(row => row.Name.Length));
        Assert.AreEqual(21, siblings.Max(row => row.Name.Length));
        Assert.AreEqual(7, root.Sum(row => CosmicQueryMetadata.Value<string>(row, "text").Length));
        Assert.AreEqual(0, root.Sum(row => (int?)CosmicQueryMetadata.Value<string>(row, "presentNull").Length));
        Assert.AreEqual(0, root.Where(row => row.Version < 0).Sum(row => row.Name.Length));
        Assert.AreEqual(14, siblings.OrderBy(row => row.Name).Take(1).Sum(row => row.Name.Length));
        Assert.IsTrue(siblings.Select(row => row.Name.Length).All(value => value > 12));
        Assert.IsFalse(siblings.Select(row => row.Name.Length).All(value => value > 13));
        Assert.AreEqual(1, siblings.Where(row => row.Description == "Case").Select(row => row.Description.Length).Distinct().Count());
        Passed("computed numeric reduction, All and Distinct remain native");

        NativeLengthProbe.Constructors = 0;
        var wrong = root.Select(row => new NativeLengthProbe { Units = CosmicQueryMetadata.Value<string>(row, "narrow").Length });
        var invalid = await wrong.ToCosmicListAsync();
        Assert.IsTrue(invalid.IsError);
        Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode);
        Assert.AreEqual("CQL_DATA_INVALID", Assert.ThrowsException<CosmicLinqException>(
            () => wrong.Take(0).All(value => true)).Code);
        Assert.AreEqual(0, NativeLengthProbe.Constructors);
        Passed("invalid input cannot become nullable output or hide behind an empty selected window");

        var required = await root.Select(row => CosmicQueryMetadata.Value<string>(row, "presentNull").Length).ToCosmicListAsync();
        Assert.IsTrue(required.IsError);
        Assert.AreEqual("CQL_DATA_INVALID", required.ErrorCode);
        Passed("required computed output rejects null source cells without partial results");
    }
}
