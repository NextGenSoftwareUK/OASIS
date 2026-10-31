using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeSingleLexemeKernelRoundsOriginalDecimalWithoutDouble()
        => WithNativeGraph(async fixture =>
        {
            var samples = new List<object>();
            void Add(string raw)
            {
                object? expected = null;
                if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                    && float.IsFinite(value) && (value != 0f || !raw.Any(c => c >= '1' && c <= '9')))
                    expected = (double)(value == 0f ? 0f : value);
                samples.Add(new Dictionary<string, object?> { ["raw"] = raw, ["expected"] = expected });
            }

            foreach (var value in new[] { float.MinValue, -float.Epsilon, 0f, 0.1f, float.Epsilon, 1f, float.MaxValue })
                Add(value.ToString("R", CultureInfo.InvariantCulture));

            // Exact midpoint and one decimal unit on either side, for signed
            // even/odd boundaries across the complete binary32 exponent range.
            foreach (var exponent in new[] { 0, 1, 25, 126, 127, 200, 253, 254 })
                foreach (var mantissa in new[] { 0, 1, 8388606, 8388607 })
                {
                    var power = exponent == 0 ? -150 : exponent - 151;
                    var coefficient = new BigInteger(2L * (mantissa + (exponent == 0 ? 0L : 8388608L)) + 1);
                    coefficient *= BigInteger.Pow(power < 0 ? 5 : 2, Math.Abs(power));
                    var scale = power < 0 ? power : 0;
                    // An extra decimal place makes the neighboring values
                    // strictly closer than any exact midpoint spacing.
                    foreach (var offset in new[] { -1, 0, 1 })
                        foreach (var sign in new[] { "", "-" })
                            Add(sign + (coefficient * 10 + offset).ToString(CultureInfo.InvariantCulture)
                                + "E" + (scale - 1).ToString(CultureInfo.InvariantCulture));
                }

            foreach (var raw in new[] { "1e-1000", "1e1000", "1e9223372036854775807" }) Add(raw);
            foreach (var raw in new[] { "01", "+1", ".1", "1.", "NaN", "Infinity", new string('1', 4097) })
                samples.Add(new Dictionary<string, object?> { ["raw"] = raw, ["expected"] = null });
            var parameters = new Dictionary<string, object> { ["samples"] = samples };
            string Bind(object value)
            {
                var name = "single" + parameters.Count;
                parameters.Add(name, value);
                return "$" + name;
            }
            var operand = NeoCqlSingle.Operand("'number'", "sample.raw", Bind);
            using var session = fixture.Driver.AsyncSession();
            using var transaction = await session.BeginTransactionAsync(c => c.WithTimeout(TimeSpan.FromSeconds(30)));
            var rows = await (await transaction.RunAsync("UNWIND $samples AS sample RETURN sample.raw AS raw,"
                + "sample.expected AS expected," + operand.Value + " AS actual", parameters)).ToListAsync();
            Assert.AreEqual(samples.Count, rows.Count);
            foreach (var row in rows)
                Assert.AreEqual(row["expected"], row["actual"], row["raw"].As<string>());
            await transaction.CommitAsync();
        });
}
