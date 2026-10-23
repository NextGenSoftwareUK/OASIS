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

    private static CosmicProjectionSelection<Holon> ProjectionColumn(CqlProjectionV1Column column)
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
                "Id" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.Id)),
                "Name" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.Name)),
                "Description" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.Description)),
                "Version" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.Version)),
                "HolonType" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.HolonType)),
                "IsActive" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.IsActive)),
                "IsPublic" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.IsPublic)),
                "ParentHolonId" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.ParentHolonId)),
                "VersionId" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.VersionId)),
                "PreviousVersionId" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.PreviousVersionId)),
                "CreatedByAvatarId" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.CreatedByAvatarId)),
                "ModifiedByAvatarId" => Required(CosmicProjectionSelection<Holon>.Create(alias, h => h.ModifiedByAvatarId)),
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
            return MetadataSelection<string>(alias, path);
        }
        var nullable = column.Nullable == true;
        return column.ValueType switch
        {
            "Boolean" => MetadataSelection<bool>(alias, path, nullable),
            "Int32" => MetadataSelection<int>(alias, path, nullable),
            "Int64" => MetadataSelection<long>(alias, path, nullable),
            "Byte" => MetadataSelection<byte>(alias, path, nullable),
            "SByte" => MetadataSelection<sbyte>(alias, path, nullable),
            "Int16" => MetadataSelection<short>(alias, path, nullable),
            "UInt16" => MetadataSelection<ushort>(alias, path, nullable),
            "UInt32" => MetadataSelection<uint>(alias, path, nullable),
            "Guid" => MetadataSelection<Guid>(alias, path, nullable),
            "DateTime" => MetadataSelection<DateTime>(alias, path, nullable),
            "HolonType" => MetadataSelection<HolonType>(alias, path, nullable),
            "ProviderType" => MetadataSelection<ProviderType>(alias, path, nullable),
            _ => throw new ArgumentException("Unsupported exact metadata projection type.")
        };
    }

    private static bool AsciiLetter(char value) => value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    private static CosmicProjectionSelection<Holon> MetadataSelection<TValue>(string alias, string[] segments, bool nullable)
        where TValue : struct
        => nullable ? MetadataSelection<TValue?>(alias, segments) : MetadataSelection<TValue>(alias, segments);
    private static CosmicProjectionSelection<Holon> MetadataSelection<TValue>(string alias, string[] segments)
    {
        var parameter = Expression.Parameter(typeof(Holon), "holon");
        var path = Expression.NewArrayInit(typeof(string), segments.Select(segment => Expression.Constant(segment)));
        var value = Expression.Call(typeof(CosmicQueryMetadata), "Value", new[] { typeof(TValue) }, parameter, path);
        return Required(CosmicProjectionSelection<Holon>.Create(alias, Expression.Lambda<Func<Holon, TValue>>(value, parameter)));
    }
    private static CosmicProjectionSelection<Holon> Required(OASISResult<CosmicProjectionSelection<Holon>> selection)
    {
        if (selection.IsError) throw new ArgumentException(selection.Message);
        return selection.Result;
    }
}
