using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        var kind=ValidateSetRequest(request);
        if(request.Left.Columns!=null || request.Right.Columns!=null || request.Left.Stages!=null || request.Right.Stages!=null)
            throw new ArgumentException("Explicit selected columns require the selected-value set compiler.");
        var query=RequiredSet(CosmicSetQuery<Holon>.Create(SetSource(request.Left,actor,provider,access),SetSource(request.Right,actor,provider,access),kind));
        if(request.Ordering!=null)
        {
            if(request.Ordering.Length is <1 or >4)throw new ArgumentException("Set ordering requires one to four terms.");
            var fields=new HashSet<string>(StringComparer.Ordinal);
            foreach(var term in request.Ordering)
            {
                if(term?.Field is not ("Name" or "Description" or "Version" or "Id") || !fields.Add(term.Field))
                    throw new ArgumentException("Set ordering requires unique declared entity fields.");
                query=CompileSelection(new CqlProjectionV1Column {Alias=term.Field,Field=term.Field},
                    new SetOrderingConsumer(query,term.Descending,fields.Count>1));
            }
            if(!fields.Contains("Id"))query=RequiredSet(query.ThenBy(row=>row.Id));
        }
        if(request.Reverse)query=RequiredSet(query.Reverse());
        if(request.Skip!=0)query=RequiredSet(query.Skip(request.Skip));
        if(request.Take.HasValue)query=RequiredSet(query.Take(request.Take.Value));
        return query;
    }
    private static CosmicSetQueryKind ValidateSetRequest(CqlSetV1Request request)
    {
        if(request?.Left==null || request.Right==null || request.Left.Graph!=null || request.Right.Graph!=null ||
            request.MaximumLeftRows is <1 or >1000 || request.MaximumRightRows is <1 or >1000 ||
            request.MaximumResults is <1 or >100 || request.Skip is <0 or >10000 ||
            request.Take is <0 or >100)
            throw new ArgumentException("Two bounded native entity sources without graph expansion are required.");
        return request.Kind switch {
            "Union"=>CosmicSetQueryKind.Union,"Concat"=>CosmicSetQueryKind.Concat,
            "Intersect"=>CosmicSetQueryKind.Intersect,"Except"=>CosmicSetQueryKind.Except,
            _=>throw new ArgumentException("Unknown native set operation.")
        };
    }
    private static CosmicQuery<Holon> SetSource(CqlSetV1Source source,Guid actor,ProviderType? provider,CosmicQueryReadAccess access)
        =>CompileSource(actor,provider,source.Filters,source.AnyOf,source.AnyChildren,source.AnyDescendants,
            source.Graph,source.OrderBy,source.Descending,source.Skip,source.Take,access,where:source.Where);
    private sealed class SetOrderingConsumer(CosmicSetQuery<Holon> source,bool descending,bool append)
        :ISelectionConsumer<CosmicSetQuery<Holon>>
    {
        public CosmicSetQuery<Holon> Bind<TValue>(string alias,Expression<Func<Holon,TValue>> selector)
            =>RequiredSet(append?(descending?source.ThenByDescending(selector):source.ThenBy(selector))
                :(descending?source.OrderByDescending(selector):source.OrderBy(selector)));
    }
    private static T RequiredSet<T>(OASISResult<T> result)
        =>result.IsError?throw new ArgumentException(result.Message):result.Result;
}
