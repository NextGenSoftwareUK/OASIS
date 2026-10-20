using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static readonly ExpressionType[] MixedNumericOperators = { ExpressionType.Equal, ExpressionType.NotEqual, ExpressionType.GreaterThan,
        ExpressionType.GreaterThanOrEqual, ExpressionType.LessThan, ExpressionType.LessThanOrEqual };

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task NativeCqlMixedNumericBoundsMatchClrAcrossInt32Domain(bool floatingPoint, bool reversed)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var versions = new[] { int.MinValue, -2, -1, 0, 1, 2, int.MaxValue - 1, int.MaxValue };
        var rows = new List<Holon>();
        foreach (var version in versions)
        {
            var row = new Holon { Id = Guid.NewGuid(), Version = version };
            var saved = await provider.SaveHolonAsync(row, saveChildren: false); saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
            var loaded = (Holon)(await provider.LoadHolonAsync(row.Id, loadChildren: false)).Result;
            loaded.Version.Should().Be(version, "the oracle fixture must retain every tested boundary value"); rows.Add(loaded);
        }
        object[] literals = floatingPoint
            ? new double[] { double.MinValue, (double)int.MinValue - 1d, int.MinValue, -2.5d, -1d, -double.Epsilon, -0d, 0d,
                double.Epsilon, 1d, Math.BitIncrement(1d), 1.5d, Math.BitDecrement((double)int.MaxValue), int.MaxValue,
                Math.BitIncrement((double)int.MaxValue), (double)int.MaxValue + 1d, double.MaxValue }.Cast<object>().ToArray()
            : new decimal[] { decimal.MinValue, (decimal)int.MinValue - 1m, int.MinValue, (decimal)int.MinValue + 0.0000000000000000001m,
                -2.5m, -1m, -0.1m, 0m, 0.1m, 1m, 1.000000000000000000000000001m, 1.5m, 2m,
                (decimal)int.MaxValue - 0.0000000000000000001m, int.MaxValue, (decimal)int.MaxValue + 1m, decimal.MaxValue }.Cast<object>().ToArray();
        foreach (var literal in literals)
        foreach (var operation in MixedNumericOperators)
        {
            var root = Expression.Parameter(typeof(Holon), "h");
            var field = Expression.Convert(Expression.Property(root, nameof(Holon.Version)), literal.GetType());
            var constant = Expression.Constant(literal, literal.GetType());
            var body = reversed ? Expression.MakeBinary(operation, constant, field) : Expression.MakeBinary(operation, field, constant);
            var predicate = Expression.Lambda<Func<Holon, bool>>(body, root);
            var expected = rows.Where(predicate.Compile()).Select(h => h.Id).OrderBy(id => id).ToArray();
            var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
            result.IsError.Should().BeFalse($"{literal} {operation} reversed={reversed}: {result.Message}{result.Exception}");
            result.Result.Select(h => h.Id).OrderBy(id => id).Should().Equal(expected, $"{literal} {operation} reversed={reversed}");
        }
    }

    [Fact]
    public async Task NativeCqlMixedEnumStorageComparisonMatchesClrForAdmittedNumericCasts()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var rows = new List<Holon>();
        foreach (var kind in new[] { HolonType.Default, HolonType.Planet, HolonType.Quest })
        {
            var row = new Holon { Id = Guid.NewGuid(), HolonType = kind };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); rows.Add(row);
        }
        foreach (var floatingPoint in new[] { false, true })
        foreach (var operation in MixedNumericOperators)
        {
            var root = Expression.Parameter(typeof(Holon), "h");
            object literal = floatingPoint ? (object)(double)(int)HolonType.Quest : (decimal)(int)HolonType.Quest;
            var field = Expression.Convert(Expression.Convert(Expression.Property(root, nameof(Holon.HolonType)), typeof(int)), literal.GetType());
            var predicate = Expression.Lambda<Func<Holon, bool>>(Expression.MakeBinary(operation, field, Expression.Constant(literal)), root);
            var expected = rows.Where(predicate.Compile()).Select(h => h.Id).OrderBy(id => id).ToArray();
            var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate)); result.IsError.Should().BeFalse(result.Message + result.Exception);
            result.Result.Select(h => h.Id).OrderBy(id => id).Should().Equal(expected);
        }
    }
}
