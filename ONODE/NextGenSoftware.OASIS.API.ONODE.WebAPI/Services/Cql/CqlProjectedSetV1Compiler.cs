using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
public static partial class CqlV1Compiler
{
    public static CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> CompileProjectedSet(
        CqlSetV1Request request,Guid actor,ProviderType? provider,CosmicQueryReadAccess access=null)
    {
        var kind=ValidateSetRequest(request);
        if(request.Left.Columns==null || request.Right.Columns==null || request.IncludeMetadata)
            throw new ArgumentException("Selected sets require explicit columns on both inputs and no entity metadata expansion.");
        CosmicProjectionQuery<Holon,CosmicProjectionRecord> Selection(CqlSetV1Source source)
        {
            if(source.Columns.Length is <1 or >32)throw new ArgumentException("Select one to32 columns per input.");
            return RequiredSet(SetSource(source,actor,provider,access).SelectColumns(source.Columns.Select(ProjectionColumn).ToArray()));
        }
        var query=RequiredSet(CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.Create(
            Selection(request.Left),Selection(request.Right),kind));
        if(request.Ordering!=null)
        {
            if(request.Ordering.Length is <1 or >4)throw new ArgumentException("Selected set ordering requires one to four aliases.");
            var aliases=new HashSet<string>(StringComparer.Ordinal);
            foreach(var term in request.Ordering)
            {
                if(term?.Field==null || !aliases.Add(term.Field))
                    throw new ArgumentException("Selected set ordering aliases must be nonempty and unique.");
                var column=query.Output.Columns.SingleOrDefault(value=>value.Alias==term.Field)
                    ??throw new ArgumentException("Selected set ordering must name an exact visible alias.");
                query=BindSelectedOrdering(column.ValueType,column.Alias,term.Descending,aliases.Count>1,
                    new ProjectedSetOrderingConsumer(query));
            }
        }
        if(request.Reverse)query=RequiredSet(query.Reverse());
        if(request.Skip!=0)query=RequiredSet(query.Skip(request.Skip));
        if(request.Take.HasValue)query=RequiredSet(query.Take(request.Take.Value));
        return query;
    }
    private sealed class ProjectedSetOrderingConsumer(CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> source)
        :ISelectedOrderingConsumer<CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>>
    {
        public CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> Bind<TValue>(
            Expression<Func<CosmicProjectionRecord,TValue>> selector,bool descending,bool append)
            =>RequiredSet(append?(descending?source.ThenByDescending(selector):source.ThenBy(selector))
                :(descending?source.OrderByDescending(selector):source.OrderBy(selector)));
    }
}
