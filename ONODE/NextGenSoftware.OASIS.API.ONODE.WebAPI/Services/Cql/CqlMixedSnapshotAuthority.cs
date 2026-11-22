using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Compiler
{
    // ID-only, owner- and mapping-bound provenance. This never reads cached payloads
    // or scans the native external source and shares the ordinary CQL read policy.
    internal static CosmicProjectionQuery<Holon, CosmicProjectionRecord> CompileSnapshotIdentityProjection(
        Guid actor, ProviderType provider, CosmicQueryReadAccess access, Guid mappingId, IReadOnlyList<Guid> nativeIds)
    {
        if (actor == Guid.Empty || mappingId == Guid.Empty)
            throw new ArgumentException("Snapshot authority requires an actor and mapping.");
        if (nativeIds == null || nativeIds.Count is < 1 or >= CqlVirtualProjection.MaximumRows ||
            nativeIds.Any(id => id == Guid.Empty) || nativeIds.Distinct().Count() != nativeIds.Count)
            throw new ArgumentException("Cache provenance requires the complete bounded native identity set.");
        var matchedIds = nativeIds.ToArray();
        var scope = CosmicQueryScope.Named(provider);
        if (scope.IsError) throw new ArgumentException(scope.Message);
        var source = Add(CosmicQuery<Holon>.Create(scope.Result), actor, access, provider);
        var row = Expression.Parameter(typeof(Holon), "snapshot");
        var owned = Expression.Equal(Expression.Property(row, nameof(Holon.CreatedByAvatarId)),
            Expression.Constant(actor));
        var boundMapping = Predicate(new CqlV1Filter {
            MetadataPath = new[] { VirtualHolonRead.SnapshotProvenanceMetadataKey,
                VirtualHolonRead.SnapshotMappingIdMetadataKey },
            Scalar = CqlV1Scalar.String, Operator = CqlV1Operator.Equal,
            Value = JsonSerializer.SerializeToElement(mappingId.ToString("D"))
        }, row);
        source = Add(source, row, Expression.AndAlso(owned, boundMapping));
        var matched = source.Where(item => matchedIds.Contains(item.Id));
        if (matched.IsError) throw new ArgumentException(matched.Message);
        var page = matched.Result.OrderBy(item => item.Id).Take(CqlVirtualProjection.MaximumRows);
        if (page.IsError) throw new ArgumentException(page.Message);
        var identity = CosmicProjectionSelection<Holon>.Create("CqlSnapshotIdentity", item => item.Id);
        if (identity.IsError) throw new ArgumentException(identity.Message);
        var selected = page.Result.SelectColumns(new[] { identity.Result });
        if (selected.IsError) throw new ArgumentException(selected.Message);
        return selected.Result;
    }
}
