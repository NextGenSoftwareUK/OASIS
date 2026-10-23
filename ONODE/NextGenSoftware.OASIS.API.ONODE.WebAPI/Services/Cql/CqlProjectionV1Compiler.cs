using System;
using System.Linq;
using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Compiler
{
    public static CosmicProjectionQuery<Holon, CosmicProjectionRecord> CompileProjection(
        CqlProjectionV1Request request, Guid actor, ProviderType? provider)
    {
        if (request == null || request.Take is < 0 or > 100 || request.Columns == null
            || request.Columns.Length is < 1 or > CosmicProjectionQuery<Holon, CosmicProjectionRecord>.MaximumColumns)
            throw new ArgumentException("Projection requires one to32 columns and Take0–100.");
        var source = CompileSource(actor, provider, request.Filters, request.AnyOf, request.AnyChildren,
            request.AnyDescendants, request.Graph, request.OrderBy, request.Descending, request.Skip, request.Take);
        var columns = request.Columns.Select(ProjectionColumn).ToArray();
        var selected = source.SelectColumns(columns);
        if (selected.IsError) throw new ArgumentException(selected.Message);
        return selected.Result;
    }

    internal interface ISelectionConsumer<TResult>
    {
        TResult Bind<TValue>(string alias, Expression<Func<Holon, TValue>> selector);
    }

    private sealed class ProjectionSelectionConsumer : ISelectionConsumer<CosmicProjectionSelection<Holon>>
    {
        public CosmicProjectionSelection<Holon> Bind<TValue>(string alias, Expression<Func<Holon, TValue>> selector)
            => Required(CosmicProjectionSelection<Holon>.Create(alias, selector));
    }

    private static CosmicProjectionSelection<Holon> ProjectionColumn(CqlProjectionV1Column column)
        => CompileSelection(column, new ProjectionSelectionConsumer());

    // Both HTTP shapes bind one bounded typed expression. No delegate is compiled
    // or invoked; the Core selector compiler remains the path authority.
    internal static TResult CompileSelection<TResult>(CqlProjectionV1Column column, ISelectionConsumer<TResult> consumer)
    {
        if (column == null || string.IsNullOrEmpty(column.Alias) || column.Alias.Length > 64
            || !AsciiLetter(column.Alias[0]) || column.Alias.Any(c => !AsciiLetter(c) && c is not (>= '0' and <= '9') && c != '_'))
            throw new ArgumentException("Column alias requires one to64 ASCII letters/digits/underscores, starting with a letter.");
        var alias = column.Alias;
        if (column.MetadataPath == null)
        {
            if (column.ValueType != null || column.Nullable.HasValue)
                throw new ArgumentException("Canonical column types are inferred and cannot be overridden.");
            return column.Field switch
            {
                "Id" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.Id)),
                "Name" => consumer.Bind(alias, (Expression<Func<Holon, string>>)(h => h.Name)),
                "Description" => consumer.Bind(alias, (Expression<Func<Holon, string>>)(h => h.Description)),
                "Version" => consumer.Bind(alias, (Expression<Func<Holon, int>>)(h => h.Version)),
                "HolonType" => consumer.Bind(alias, (Expression<Func<Holon, HolonType>>)(h => h.HolonType)),
                "IsActive" => consumer.Bind(alias, (Expression<Func<Holon, bool>>)(h => h.IsActive)),
                "IsPublic" => consumer.Bind(alias, (Expression<Func<Holon, bool>>)(h => h.IsPublic)),
                "ParentHolonId" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.ParentHolonId)),
                "VersionId" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.VersionId)),
                "PreviousVersionId" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.PreviousVersionId)),
                "CreatedByAvatarId" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.CreatedByAvatarId)),
                "ModifiedByAvatarId" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.ModifiedByAvatarId)),
                _ => throw new ArgumentException("Unsupported canonical projection field.")
            };
        }
        if (column.Field != null || column.MetadataPath.Length is < 1 or > 4
            || column.MetadataPath.Any(segment => string.IsNullOrEmpty(segment) || segment.Length > 64 || segment.Any(char.IsControl)))
            throw new ArgumentException("Select exactly one field or a metadata path of one to4 bounded segments.");
        var path = (string[])column.MetadataPath.Clone();
        if (column.ValueType == "String")
        {
            if (column.Nullable == false) throw new ArgumentException("String projection retains the nullable string contract.");
            return MetadataSelection<string, TResult>(alias, path, consumer);
        }
        var nullable = column.Nullable == true;
        return column.ValueType switch
        {
            "Boolean" => MetadataSelection<bool, TResult>(alias, path, nullable, consumer),
            "Int32" => MetadataSelection<int, TResult>(alias, path, nullable, consumer),
            "Int64" => MetadataSelection<long, TResult>(alias, path, nullable, consumer),
            "Byte" => MetadataSelection<byte, TResult>(alias, path, nullable, consumer),
            "SByte" => MetadataSelection<sbyte, TResult>(alias, path, nullable, consumer),
            "Int16" => MetadataSelection<short, TResult>(alias, path, nullable, consumer),
            "UInt16" => MetadataSelection<ushort, TResult>(alias, path, nullable, consumer),
            "UInt32" => MetadataSelection<uint, TResult>(alias, path, nullable, consumer),
            "Guid" => MetadataSelection<Guid, TResult>(alias, path, nullable, consumer),
            "DateTime" => MetadataSelection<DateTime, TResult>(alias, path, nullable, consumer),
            "HolonType" => MetadataSelection<HolonType, TResult>(alias, path, nullable, consumer),
            "ProviderType" => MetadataSelection<ProviderType, TResult>(alias, path, nullable, consumer),
            _ => throw new ArgumentException("Unsupported exact metadata projection type.")
        };
    }

    private static bool AsciiLetter(char value) => value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    private static TResult MetadataSelection<TValue, TResult>(string alias, string[] segments, bool nullable,
        ISelectionConsumer<TResult> consumer) where TValue : struct
        => nullable ? MetadataSelection<TValue?, TResult>(alias, segments, consumer)
            : MetadataSelection<TValue, TResult>(alias, segments, consumer);

    private static TResult MetadataSelection<TValue, TResult>(string alias, string[] segments, ISelectionConsumer<TResult> consumer)
    {
        var parameter = Expression.Parameter(typeof(Holon), "holon");
        var path = Expression.NewArrayInit(typeof(string), segments.Select(segment => Expression.Constant(segment)));
        var value = Expression.Call(typeof(CosmicQueryMetadata), "Value", new[] { typeof(TValue) }, parameter, path);
        return consumer.Bind(alias, Expression.Lambda<Func<Holon, TValue>>(value, parameter));
    }
    private static CosmicProjectionSelection<Holon> Required(OASISResult<CosmicProjectionSelection<Holon>> selection)
    {
        if (selection.IsError) throw new ArgumentException(selection.Message);
        return selection.Result;
    }
}
