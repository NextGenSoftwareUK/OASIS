using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
public static partial class CqlV1Compiler
{
    public static CosmicSetQuery<Holon> CompileSet(CqlSetV1Request request, Guid actor,
        ProviderType? provider, CosmicQueryReadAccess access = null)
    {
        if(request?.Left==null || request.Right==null || request.Left.Graph!=null || request.Right.Graph!=null ||
            request.MaximumLeftRows is <1 or >1000 || request.MaximumRightRows is <1 or >1000 ||
            request.MaximumResults is <1 or >100 || request.Skip is <0 or >10000 ||
            request.Take is <0 or >100)
            throw new ArgumentException("Two bounded native entity sources without graph expansion are required.");
        var kind=request.Kind switch {
            "Union"=>CosmicSetQueryKind.Union,"Concat"=>CosmicSetQueryKind.Concat,
            "Intersect"=>CosmicSetQueryKind.Intersect,"Except"=>CosmicSetQueryKind.Except,
            _=>throw new ArgumentException("Unknown native set operation.")
        };
        CosmicQuery<Holon> Source(CqlSetV1Source source)=>CompileSource(actor,provider,source.Filters,source.AnyOf,
            source.AnyChildren,source.AnyDescendants,source.Graph,source.OrderBy,source.Descending,source.Skip,source.Take,access,where:source.Where);
        var query=RequiredSet(CosmicSetQuery<Holon>.Create(Source(request.Left),Source(request.Right),kind));
        if(request.Reverse)query=RequiredSet(query.Reverse());
        if(request.Skip!=0)query=RequiredSet(query.Skip(request.Skip));
        if(request.Take.HasValue)query=RequiredSet(query.Take(request.Take.Value));
        return query;
    }
    private static T RequiredSet<T>(OASISResult<T> result)
        =>result.IsError?throw new ArgumentException(result.Message):result.Result;
}
