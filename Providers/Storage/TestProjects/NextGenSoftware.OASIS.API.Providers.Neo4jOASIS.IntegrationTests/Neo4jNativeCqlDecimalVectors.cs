using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeDecimalLexemeKernelMatchesCoreWithoutFloatingPoint()
        => WithNativeGraph(async fixture =>
        {
            var samples = new List<object>();
            void Add(string raw, decimal? value) => samples.Add(new Dictionary<string, object?>
            {
                ["raw"] = raw,
                ["expected"] = value.HasValue ? CosmicNumericScalarResults.DecimalOrderKey(value.Value) : null
            });

            var random = new Random(20261024);
            for (var index = 0; index < 1024; index++)
            {
                var value = new decimal(random.Next(), random.Next(), random.Next(),
                    index % 2 == 0, (byte)(index % 29));
                Add(value.ToString(CultureInfo.InvariantCulture), value);
                Add(value.ToString("E28", CultureInfo.InvariantCulture), value);
            }

            foreach (var value in new[] { decimal.MinValue, decimal.MaxValue, 0m, -1e-28m, 1e-28m })
                Add(value.ToString(CultureInfo.InvariantCulture), value);

            Add("-0e999999999999999999999999999999999", 0m);
            Add("1e+" + new string('0', 400), 1m);
            foreach (var raw in new[] { "1e-1000", "1e1000", "79228162514264337593543950336",
                "1.00000000000000000000000000001", "01", "+1", ".1", "1.", "1e", "NaN", "Infinity", "1e9223372036854775807" })
                Add(raw, null);

            var parameters = new Dictionary<string, object> { ["samples"] = samples };
            string Bind(object value)
            {
                var name = "decimal" + parameters.Count;
                parameters.Add(name, value);
                return "$" + name;
            }

            var operand = NeoCqlDecimal.Operand("'number'", "sample.raw", Bind);
            using var session = fixture.Driver.AsyncSession();
            using var transaction = await session.BeginTransactionAsync(config => config.WithTimeout(TimeSpan.FromSeconds(30)));
            var rows = await (await transaction.RunAsync("UNWIND $samples AS sample RETURN sample.raw AS raw,"
                + "sample.expected AS expected," + operand.Value + " AS actual", parameters)).ToListAsync();

            Assert.AreEqual(samples.Count, rows.Count);
            foreach (var row in rows)
                Assert.AreEqual(row["expected"], row["actual"], row["raw"].As<string>());

            await transaction.CommitAsync();
        });
}
