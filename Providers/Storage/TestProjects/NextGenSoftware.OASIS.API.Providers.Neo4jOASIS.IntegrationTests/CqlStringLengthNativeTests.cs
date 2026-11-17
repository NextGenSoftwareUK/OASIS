using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyNativeStringLength(
        ICosmicQueryStorageProvider adapter, Guid[] ids, List<object> checks)
    {
        const string prefix = "length-";
        var values = new[] { "", "x", "\U0001F600", "e\u0301", "abc\u0000def" };
        Assert.AreEqual(values.Length, ids.Length);
        var storage = (IOASISStorageProvider)adapter;
        for (var index = 0; index < ids.Length; index++)
        {
            var loaded = await storage.LoadHolonAsync(ids[index], loadChildren: false, recursive: false);
            Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
            Assert.IsNotNull(loaded.Result);
            var holon = loaded.Result;
            holon.MetaData ??= new Dictionary<string, object>();
            holon.Name = prefix + values[index];
            holon.MetaData["lengthValue"] = values[index];
            if (index == 0) holon.MetaData["notString"] = 3L;
            else if (index == 1) holon.MetaData["notString"] = null;
            else if (index == 2) holon.MetaData["notString"] = true;
            else holon.MetaData.Remove("notString");
            var saved = await storage.SaveHolonAsync(holon, saveChildren: false, recursive: false);
            Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
        }

        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => ids.Contains(row.Id)).Result;
        var native = CosmicLinq.Create(NativeManager(adapter), adapter, source, maximumResults: 10);
        Assert.IsFalse(native.IsError, native.Message);

        async Task<(Guid[] Ids, long Milliseconds)> Read(IQueryable<Holon> query)
        {
            var watch = Stopwatch.StartNew();
            var result = await query.ToCosmicListAsync();
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            return (result.Result.Select(row => row.Id).ToArray(), watch.ElapsedMilliseconds);
        }

        Expression<Func<Holon, bool>> Predicate(bool canonical, ExpressionType operation, long bound)
        {
            Expression<Func<Holon, int>> length = canonical
                ? row => row.Name.Length
                : row => CosmicQueryMetadata.Value<string>(row, "lengthValue").Length;
            return Expression.Lambda<Func<Holon, bool>>(Expression.MakeBinary(operation,
                Expression.Convert(length.Body, typeof(long)), Expression.Constant(bound)), length.Parameters);
        }

        static bool Matches(long units, ExpressionType operation, long bound) => operation switch
        {
            ExpressionType.Equal => units == bound,
            ExpressionType.NotEqual => units != bound,
            ExpressionType.GreaterThan => units > bound,
            ExpressionType.GreaterThanOrEqual => units >= bound,
            ExpressionType.LessThan => units < bound,
            ExpressionType.LessThanOrEqual => units <= bound,
            _ => throw new ArgumentException("Unknown string length test relation.")
        };

        (ExpressionType Operation, long Bound)[] cases =
        {
            (ExpressionType.Equal, -1), (ExpressionType.Equal, 0), (ExpressionType.Equal, 1),
            (ExpressionType.Equal, 2), (ExpressionType.Equal, 7), (ExpressionType.NotEqual, 2),
            (ExpressionType.GreaterThan, 1), (ExpressionType.GreaterThanOrEqual, 2),
            (ExpressionType.LessThan, 2), (ExpressionType.LessThanOrEqual, 2),
            (ExpressionType.Equal, long.MaxValue), (ExpressionType.GreaterThan, long.MinValue)
        };
        foreach (var canonical in new[] { false, true })
        {
            foreach (var sample in cases)
            {
                var bound = sample.Bound;
                if (canonical && bound != long.MinValue && bound != long.MaxValue) bound += prefix.Length;
                var expected = Enumerable.Range(0, values.Length)
                    .Where(index => Matches(values[index].Length + (canonical ? prefix.Length : 0),
                        sample.Operation, bound)).Select(index => ids[index]).ToArray();
                var actual = await Read(native.Result.Where(Predicate(canonical, sample.Operation, bound)));
                CollectionAssert.AreEquivalent(expected, actual.Ids);
                checks.Add(new { provider = adapter.QueryProviderType.ToString(), utf16Length = true,
                    canonical, relation = sample.Operation.ToString(), bound, elapsedMilliseconds = actual.Milliseconds });
            }
            var page = await Read(native.Result.Where(Predicate(canonical, ExpressionType.Equal,
                2 + (canonical ? prefix.Length : 0))).OrderBy(row => row.Name).Skip(1).Take(1));
            CollectionAssert.AreEqual(new[] { ids[2] }, page.Ids);
            checks.Add(new { provider = adapter.QueryProviderType.ToString(), utf16Length = "filter before paging",
                canonical, elapsedMilliseconds = page.Milliseconds });
        }

        var wrong = await Read(native.Result.Where(row =>
            CosmicQueryMetadata.Value<string>(row, "notString").Length >= 0));
        Assert.AreEqual(0, wrong.Ids.Length);
        checks.Add(new { provider = adapter.QueryProviderType.ToString(),
            utf16Length = "missing, null and wrong scalar domains", elapsedMilliseconds = wrong.Milliseconds });
        await VerifyNativeStringLengthWire(adapter, ids, checks);
    }
}
