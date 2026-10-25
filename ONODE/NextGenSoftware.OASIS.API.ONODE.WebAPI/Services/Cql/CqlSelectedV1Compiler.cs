using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Compiler
{
    private static Expression<Func<CosmicProjectionRecord, bool>> CompileSelectedFilter(
        Type selectedType, string alias, CqlV1Operator operation, JsonElement json, ParameterExpression row = null)
    {
        if (!Enum.IsDefined(operation) || operation == CqlV1Operator.Exists)
            throw new ArgumentException("A defined selected comparison is required; selected aliases always exist.");
        row ??= Expression.Parameter(typeof(CosmicProjectionRecord), "row");
        var cell = Expression.Call(row, typeof(CosmicProjectionRecord).GetMethod("get_Item"), Expression.Constant(alias));
        var path = Expression.Convert(cell, selectedType);
        var type = Nullable.GetUnderlyingType(selectedType) ?? selectedType;
        object value;
        if (operation == CqlV1Operator.IsNull)
        {
            if (json.ValueKind != JsonValueKind.Undefined)
                throw new ArgumentException("IsNull does not accept Value.");
            value = null;
        }
        else if (json.ValueKind == JsonValueKind.Null) value = null;
        else if (type == typeof(string) && json.ValueKind == JsonValueKind.String && json.GetString().Length <= 512)
            value = json.GetString();
        else if (type == typeof(bool) && json.ValueKind is JsonValueKind.True or JsonValueKind.False)
            value = json.GetBoolean();
        // Int64 and Decimal use invariant strings to preserve precision through JavaScript clients.
        else if (type == typeof(long) && json.ValueKind == JsonValueKind.String &&
            long.TryParse(json.GetString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer)) value = integer;
        else if (type == typeof(float) && TrySingle(json, out var finiteSingle)) value = finiteSingle;
        else if (type == typeof(double) && TryDouble(json, out var finiteDouble)) value = finiteDouble;
        else if (type == typeof(decimal) && TryDecimal(json, out var number)) value = number;
        else if (type == typeof(int) && json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out var count)) value = count;
        else if (type == typeof(byte) && json.ValueKind == JsonValueKind.Number && json.TryGetByte(out var u8)) value = u8;
        else if (type == typeof(sbyte) && json.ValueKind == JsonValueKind.Number && json.TryGetSByte(out var i8)) value = i8;
        else if (type == typeof(short) && json.ValueKind == JsonValueKind.Number && json.TryGetInt16(out var i16)) value = i16;
        else if (type == typeof(ushort) && json.ValueKind == JsonValueKind.Number && json.TryGetUInt16(out var u16)) value = u16;
        else if (type == typeof(uint) && json.ValueKind == JsonValueKind.Number && json.TryGetUInt32(out var u32)) value = u32;
        else if ((type == typeof(HolonType) || type == typeof(ProviderType)) && json.ValueKind == JsonValueKind.String
            && json.GetString().Length <= 40 && Enum.TryParse(type, json.GetString(), false, out var member)
            && Enum.IsDefined(type, member) && member.ToString() == json.GetString()) value = member;
        else if (type == typeof(Guid) && json.ValueKind == JsonValueKind.String &&
            Guid.TryParseExact(json.GetString(), "D", out var guid)) value = guid;
        else if (type == typeof(ulong) && TryUInt64(json, out var unsigned)) value = unsigned;
        else if (type == typeof(DateTime) && TryUtcDate(json, out var utc)) value = utc;
        else throw new ArgumentException("Value must match the exact selected scalar type; Int64, UInt64, Decimal, Double and Single require invariant strings.");
        if (value == null && type == selectedType && type.IsValueType)
            throw new ArgumentException("This selected column cannot be null.");
        Expression literal = value == null ? Expression.Constant(null, selectedType)
            : Expression.Constant(value, type);
        if (literal.Type != path.Type) literal = Expression.Convert(literal, path.Type);
        Expression predicate;
        if (operation is CqlV1Operator.Contains or CqlV1Operator.StartsWith or CqlV1Operator.EndsWith)
        {
            if (type != typeof(string) || value == null)
                throw new ArgumentException("String operations require a non-null string literal.");
            var method = typeof(string).GetMethod(operation.ToString(), new[] { typeof(string), typeof(StringComparison) });
            predicate = Expression.Call(path, method, literal, Expression.Constant(StringComparison.Ordinal));
        }
        else
        {
            var binaryOperation = operation switch
            {
                CqlV1Operator.Equal or CqlV1Operator.IsNull => ExpressionType.Equal,
                CqlV1Operator.NotEqual => ExpressionType.NotEqual,
                CqlV1Operator.GreaterThan => ExpressionType.GreaterThan,
                CqlV1Operator.GreaterThanOrEqual => ExpressionType.GreaterThanOrEqual,
                CqlV1Operator.LessThan => ExpressionType.LessThan,
                CqlV1Operator.LessThanOrEqual => ExpressionType.LessThanOrEqual,
                _ => throw new ArgumentException("Unsupported selected filter operation.")
            };
            try { predicate = Expression.MakeBinary(binaryOperation, path, literal); }
            catch (InvalidOperationException exception) { throw new ArgumentException("The selected type does not support this comparison.", exception); }
        }
        return Expression.Lambda<Func<CosmicProjectionRecord, bool>>(predicate, row);
    }

    private interface ISelectedOrderingConsumer<TResult>
    {
        TResult Bind<TValue>(Expression<Func<CosmicProjectionRecord, TValue>> selector, bool descending, bool append);
    }
    private static TResult BindSelectedOrdering<TResult>(Type type, string alias, bool descending, bool append,
        ISelectedOrderingConsumer<TResult> consumer)
    {
        // Bind the exact selected CLR type. This constructs an expression for the
        // shared Core owner; it never compiles or executes a selector delegate.
        if (type == typeof(string)) return Bind<string>();
        if (type == typeof(bool)) return Bind<bool>();
        if (type == typeof(bool?)) return Bind<bool?>();
        if (type == typeof(int)) return Bind<int>();
        if (type == typeof(int?)) return Bind<int?>();
        if (type == typeof(long)) return Bind<long>();
        if (type == typeof(long?)) return Bind<long?>();
        if (type == typeof(ulong)) return Bind<ulong>();
        if (type == typeof(ulong?)) return Bind<ulong?>();
        if (type == typeof(Guid)) return Bind<Guid>();
        if (type == typeof(Guid?)) return Bind<Guid?>();
        if (type == typeof(DateTime)) return Bind<DateTime>();
        if (type == typeof(DateTime?)) return Bind<DateTime?>();
        if (type == typeof(float)) return Bind<float>();
        if (type == typeof(float?)) return Bind<float?>();
        if (type == typeof(double)) return Bind<double>();
        if (type == typeof(double?)) return Bind<double?>();
        if (type == typeof(decimal)) return Bind<decimal>();
        if (type == typeof(decimal?)) return Bind<decimal?>();
        if (type == typeof(byte)) return Bind<byte>();
        if (type == typeof(byte?)) return Bind<byte?>();
        if (type == typeof(sbyte)) return Bind<sbyte>();
        if (type == typeof(sbyte?)) return Bind<sbyte?>();
        if (type == typeof(short)) return Bind<short>();
        if (type == typeof(short?)) return Bind<short?>();
        if (type == typeof(ushort)) return Bind<ushort>();
        if (type == typeof(ushort?)) return Bind<ushort?>();
        if (type == typeof(uint)) return Bind<uint>();
        if (type == typeof(uint?)) return Bind<uint?>();
        if (type == typeof(HolonType)) return Bind<HolonType>();
        if (type == typeof(HolonType?)) return Bind<HolonType?>();
        if (type == typeof(ProviderType)) return Bind<ProviderType>();
        if (type == typeof(ProviderType?)) return Bind<ProviderType?>();
        throw new ArgumentException("This selected output domain cannot be ordered by HTTP v1.");

        TResult Bind<TValue>()
        {
            var row = Expression.Parameter(typeof(CosmicProjectionRecord), "row");
            var cell = Expression.Call(row, typeof(CosmicProjectionRecord).GetMethod("get_Item"), Expression.Constant(alias));
            var selector = Expression.Lambda<Func<CosmicProjectionRecord, TValue>>(Expression.Convert(cell, typeof(TValue)), row);
            return consumer.Bind(selector, descending, append);
        }
    }
}
