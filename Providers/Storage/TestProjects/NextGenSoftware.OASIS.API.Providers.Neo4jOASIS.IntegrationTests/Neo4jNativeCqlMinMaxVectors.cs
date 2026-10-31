using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    public static IEnumerable<object[]> MinMaxReferenceVectors()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CqlMinMaxVectors.json")));
        return document.RootElement.GetProperty("vectors").EnumerateArray()
            .Where(vector => vector.GetProperty("domain").GetString() != "DateTime")
            .Select(vector => new object[] { vector.GetProperty("name").GetString()!, vector.GetRawText() }).ToArray();
    }

    [DataTestMethod]
    [DynamicData(nameof(MinMaxReferenceVectors), DynamicDataSourceType.Method)]
    public async Task NativeMinMaxConsumesReferenceVector(string name, string json)
    {
        using var document = JsonDocument.Parse(json);
        var vector = document.RootElement; var domain = vector.GetProperty("domain").GetString()!;
        var optional = vector.GetProperty("nullable").GetBoolean();
        var inputs = vector.GetProperty("inputs").EnumerateArray().ToArray();
        await WithNativeGraph(async fixture =>
        {
            var keys = new[] { fixture.Root }.Concat(fixture.Root.Children).Take(inputs.Length).Select(row => row.Id).ToArray();
            var source = CosmicQuery<Holon>.Create().Where(h => keys.Contains(h.Id)).Result;
            var expected = vector.GetProperty("expected");
            switch (domain)
            {
                case "Int64": if (optional) await CheckVector<long?>(); else await CheckVector<long>(); break;
                case "Int32": if (optional) await CheckVector<int?>(); else await CheckVector<int>(); break;
                case "Byte": if (optional) await CheckVector<byte?>(); else await CheckVector<byte>(); break;
                case "Boolean": if (optional) await CheckVector<bool?>(); else await CheckVector<bool>(); break;
                case "String": await CheckVector<string>(); break;
                case "Guid": if (optional) await CheckVector<Guid?>(); else await CheckVector<Guid>(); break;
                case "DateTime": if (optional) await CheckVector<DateTime?>(); else await CheckVector<DateTime>(); break;
                default: throw new InvalidOperationException("Unregistered test vector domain.");
            }

            async Task CheckVector<TValue>()
            {
                foreach (var minimum in new[] { true, false })
                {
                    var intent = minimum ? source.Min(h => CosmicQueryMetadata.Value<TValue>(h, "minmaxValue"))
                        : source.Max(h => CosmicQueryMetadata.Value<TValue>(h, "minmaxValue"));
                    Assert.IsFalse(intent.IsError, intent.Message);
                    var result = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(intent.Result));
                    if (expected.TryGetProperty("error", out var error))
                    {
                        Assert.AreEqual(error.GetString(), result.ErrorCode); Assert.IsFalse(result.IsLoaded);
                    }
                    else
                    {
                        Assert.IsFalse(result.IsError, result.Message + result.Exception); Assert.IsTrue(result.IsLoaded);
                        var value = expected.GetProperty(minimum ? "min" : "max");
                        if (value.ValueKind == JsonValueKind.Null) Assert.IsNull((object?)result.Result);
                        else
                        {
                            var native = VectorStoredValue(domain, value);
                            var type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
                            if (native is long integer && type != typeof(long)) native = Convert.ChangeType(integer, type, CultureInfo.InvariantCulture);
                            Assert.AreEqual((TValue)native, result.Result);
                        }
                    }
                }
            }
        }, width: Math.Max(0, inputs.Length - 1), configure: root =>
        {
            var rows = new[] { root }.Concat(root.Children).Take(inputs.Length).ToArray();
            for (var index = 0; index < inputs.Length; index++)
            {
                var input = inputs[index]; var state = input.GetProperty("state").GetString();
                if (state != "missing") rows[index].MetaData["minmaxValue"] = state == "null" ? null!
                    : state == "wrong-domain" ? input.GetProperty("value").GetString()!
                    : VectorStoredValue(domain, input.GetProperty("value"));
            }
        });
    }

    private static object VectorStoredValue(string domain, JsonElement value) => domain switch
    {
        "Int64" or "Int32" or "Byte" => long.Parse(value.GetString()!, CultureInfo.InvariantCulture),
        "Boolean" => value.GetBoolean(), "String" => value.GetString()!,
        "Guid" => Guid.ParseExact(value.GetString()!, "D"),
        "DateTime" => DateTime.ParseExact(value.GetString()!, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        _ => throw new InvalidOperationException("Unsupported test storage domain.")
    };
}
