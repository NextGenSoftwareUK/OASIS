using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static CosmicScalarPlan<T, TResult> NumericPlan<T, TResult>(CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        var claims = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, Provider.CqlProjectionCapabilities),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, Provider.CqlProjectionCapabilities)
        });
        var plan = CosmicScalarPlanner.Plan(query, Core.Enums.ProviderType.Neo4jOASIS,
            new[] { new CosmicScalarTarget(Core.Enums.ProviderType.Neo4jOASIS, Provider.CqlCapabilities, claims) });
        Assert.IsFalse(plan.IsError, plan.Message); return plan.Result;
    }

    public static IEnumerable<object[]> NumericReferenceVectors()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CqlNumericAggregateVectors.json")));
        return document.RootElement.GetProperty("vectors").EnumerateArray()
            .Select(v => new object[] { v.GetProperty("name").GetString()!, v.GetRawText() }).ToArray();
    }

    [DataTestMethod]
    [DynamicData(nameof(NumericReferenceVectors), DynamicDataSourceType.Method)]
    public async Task NativeNumericConsumesSharedReferenceVector(string name, string json)
    {
        using var document = JsonDocument.Parse(json); var vector = document.RootElement;
        var inputs = vector.GetProperty("inputs").EnumerateArray().Select(v => long.Parse(v.GetString()!, CultureInfo.InvariantCulture)).ToArray();
        await WithNativeGraph(async fixture =>
        {
            var keys = new[] { fixture.Root }.Concat(fixture.Root.Children).Take(inputs.Length).Select(h => h.Id).ToArray();
            var source = CosmicQuery<Holon>.Create().Where(h => keys.Contains(h.Id)).Result;
            var optional = vector.GetProperty("nullable").GetBoolean();
            var average = vector.GetProperty("operation").GetString() == "Average";
            if (vector.GetProperty("domain").GetString() == "Int32")
            {
                if (optional) { if (average) await Check(source.Average(h => CosmicQueryMetadata.Value<int?>(h, "numeric")).Result); else await Check(source.Sum(h => CosmicQueryMetadata.Value<int?>(h, "numeric")).Result); }
                else { if (average) await Check(source.Average(h => CosmicQueryMetadata.Value<int>(h, "numeric")).Result); else await Check(source.Sum(h => CosmicQueryMetadata.Value<int>(h, "numeric")).Result); }
            }
            else
            {
                if (optional) { if (average) await Check(source.Average(h => CosmicQueryMetadata.Value<long?>(h, "numeric")).Result); else await Check(source.Sum(h => CosmicQueryMetadata.Value<long?>(h, "numeric")).Result); }
                else { if (average) await Check(source.Average(h => CosmicQueryMetadata.Value<long>(h, "numeric")).Result); else await Check(source.Sum(h => CosmicQueryMetadata.Value<long>(h, "numeric")).Result); }
            }

            async Task Check<TResult>(CosmicScalarQuery<Holon, TResult> intent)
            {
                var result = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(intent));
                var expected = vector.GetProperty("expected");
                if (expected.TryGetProperty("error", out var error))
                { Assert.AreEqual(error.GetString(), result.ErrorCode); Assert.IsFalse(result.IsLoaded); return; }
                Assert.IsFalse(result.IsError, result.Message + result.Exception); Assert.IsTrue(result.IsLoaded);
                var value = expected.GetProperty("value");
                if (value.ValueKind == JsonValueKind.Null) Assert.IsNull((object?)result.Result);
                else Assert.AreEqual((TResult)Convert.ChangeType(value.GetString(), Nullable.GetUnderlyingType(typeof(TResult)) ?? typeof(TResult), CultureInfo.InvariantCulture)!, result.Result);
            }
        }, width: Math.Max(0, inputs.Length - 1), configure: root =>
        {
            var rows = new[] { root }.Concat(root.Children).Take(inputs.Length).ToArray();
            for (var index = 0; index < inputs.Length; index++) rows[index].MetaData["numeric"] = inputs[index];
        });
    }
}
