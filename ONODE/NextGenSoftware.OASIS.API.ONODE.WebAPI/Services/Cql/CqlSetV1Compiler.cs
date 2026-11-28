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
        if(request?.OutputColumns!=null || request?.SelectedStages!=null)
            throw new ArgumentException("Late output columns and selected stages require the entity-set projection compiler.");
        return CompileEntitySetSource(request,actor,provider,access);
    }
    private static CosmicSetQuery<Holon> CompileEntitySetSource(CqlSetV1Request request,Guid actor,
        ProviderType? provider,CosmicQueryReadAccess access,bool reduction=false)
    {
        _=ValidateSetRequest(request,reduction);
        if(UsesSelectedSet(request))throw new ArgumentException("Selected columns require the selected-value set compiler.");
        CosmicSetQuery<Holon> Node(CqlSetV1Node node)
        {
            CosmicSetInput<Holon> Input(CqlSetV1Source source)=>source.Set==null
                ?RequiredSet(CosmicSetInput<Holon>.FromSource(SetSource(source,actor,provider,access)))
                :RequiredSet(CosmicSetInput<Holon>.FromSet(Node(source.Set)));
            var query=RequiredSet(CosmicSetQuery<Holon>.CreateTree(Input(node.Left),Input(node.Right),ParseNativeSetKind(node.Kind)));
            query=ApplyEntitySetOrdering(query,node.Ordering);
            if(node.Reverse)query=RequiredSet(query.Reverse());
            if(node.Skip!=0)query=RequiredSet(query.Skip(node.Skip));
            if(node.Take.HasValue)query=RequiredSet(query.Take(node.Take.Value));
            return ApplyEntitySetStages(query,node.EntityStages);
        }
        return Node(RootSetNode(request,false));
    }

    private static CosmicSetQuery<Holon> ApplyEntitySetOrdering(CosmicSetQuery<Holon> query,CqlV1Ordering[] ordering)
    {
        if(ordering!=null)
        {
            if(ordering.Length is <1 or >4)throw new ArgumentException("Set ordering requires one to four terms.");
            var fields=new HashSet<string>(StringComparer.Ordinal);
            foreach(var term in ordering)
            {
                if(term?.Field is not ("Name" or "Description" or "Version" or "Id") || !fields.Add(term.Field))
                    throw new ArgumentException("Set ordering requires unique declared entity fields.");
                query=CompileSelection(new CqlProjectionV1Column {Alias=term.Field,Field=term.Field},
                    new SetOrderingConsumer(query,term.Descending,fields.Count>1));
            }
            if(!fields.Contains("Id"))query=RequiredSet(query.ThenBy(row=>row.Id));
        }
        return query;
    }
    private static CosmicSetQuery<Holon> ApplyEntitySetStages(CosmicSetQuery<Holon> query,CqlSetV1EntityStage[] stages)
    {
        if(stages==null)return query;
        if(stages.Length is <1 or >16)throw new ArgumentException("Use one to16 ordered entity stages.");
        int filters=0,relationships=0;
        foreach(var stage in stages)
        {
            if(stage==null)throw new ArgumentException("An entity stage cannot be null.");
            switch(stage.Operation)
            {
                case "Where":
                    if(stage.Where==null || stage.Ordering!=null || stage.Count!=null)
                        throw new ArgumentException("Entity Where requires only a bounded predicate.");
                    ValidateWhere(stage.Where,ref filters,ref relationships);
                    var row=Expression.Parameter(typeof(Holon),"row");
                    query=RequiredSet(query.Where(Expression.Lambda<Func<Holon,bool>>(WhereExpression(stage.Where,row),row)));
                    break;
                case "OrderBy":
                    if(stage.Where!=null || stage.Count!=null || stage.Ordering==null)
                        throw new ArgumentException("Entity OrderBy requires only declared ordering terms.");
                    query=ApplyEntitySetOrdering(query,stage.Ordering);
                    break;
                case "Skip":
                case "Take":
                    if(stage.Where!=null || stage.Ordering!=null || !stage.Count.HasValue ||
                        stage.Count<0 || stage.Count>(stage.Operation=="Take"?100:10000))
                        throw new ArgumentException("Entity Skip/Take requires only a bounded nonnegative Count.");
                    query=RequiredSet(stage.Operation=="Take"?query.Take(stage.Count.Value):query.Skip(stage.Count.Value));
                    break;
                case "Reverse":
                    if(stage.Where!=null || stage.Ordering!=null || stage.Count!=null)
                        throw new ArgumentException("Entity Reverse has no additional fields.");
                    query=RequiredSet(query.Reverse());break;
                default:throw new ArgumentException("Unknown entity set stage.");
            }
        }
        return query;
    }
    private static CosmicSetQueryKind ValidateSetRequest(CqlSetV1Request request,bool reduction=false)
    {
        if(reduction!=(request?.Reduction!=null) || !reduction && request?.ReductionWhere!=null)
            throw new ArgumentException("Scalar set reductions require their independent compiler.");
        if(reduction && (request.Reduction is not ("Count" or "LongCount" or "Any" or "All" or "Sum" or "Average")
            || request.Reduction=="All" && request.ReductionWhere==null
            || request.IncludeMetadata || request.OutputColumns!=null || request.SelectedStages!=null&&!UsesSelectedSet(request)))
            throw new ArgumentException("A native entity set reduction and unambiguous scalar output are required.");
        if(request?.Left==null || request.Right==null || request.Left.Graph!=null || request.Right.Graph!=null ||
            request.MaximumLeftRows is <1 or >1000 || request.MaximumRightRows is <1 or >1000 ||
            request.MaximumResults is <1 or >100 || request.Skip is <0 or >10000 ||
            request.Take is <0 or >100)
            throw new ArgumentException("Two bounded native entity sources without graph expansion are required.");
        var selected=UsesSelectedSet(request);
        if(request.Reduction is "Sum" or "Average")
        {
            if(!selected||string.IsNullOrEmpty(request.ReductionColumn)||request.ReductionColumn.Length>128)
                throw new ArgumentException("Numeric set reduction requires one visible selected alias.");
        }
        else if(request.ReductionColumn!=null)throw new ArgumentException("Only selected numeric reductions accept ReductionColumn.");
        return ParseNativeSetKind(request.Kind);
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
