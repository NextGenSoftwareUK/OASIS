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
        =>CompileProjectedSetSource(request,actor,provider,access,false);
    private static CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> CompileProjectedSetSource(
        CqlSetV1Request request,Guid actor,ProviderType? provider,CosmicQueryReadAccess access,bool reduction)
    {
        _=ValidateSetRequest(request,reduction);
        if(request.OutputColumns!=null||request.EntityStages!=null||!UsesSelectedSet(request)||request.IncludeMetadata)
            throw new ArgumentException("Selected sets require explicit selected leaves and no entity metadata expansion.");
        CosmicProjectedSetInput<Holon,CosmicProjectionRecord> Leaf(CqlSetV1Source source)
        {
            if(source.Columns==null||source.Columns.Length is <1 or >32)throw new ArgumentException("Select one to32 columns per input.");
            var captured=SetSource(source,actor,provider,access);var columns=source.Columns.Select(ProjectionColumn).ToArray();
            if(source.EntityStages!=null)
            {
                var entity=ApplyEntitySourceStages(RequiredSet(captured.Compose()),source.EntityStages);
                var entitySelection=RequiredSet(entity.SelectColumns(columns));
                if(source.Stages!=null)entitySelection=RequiredSet(entitySelection.WithSelectedPipeline(ApplyCompositionStages(entitySelection.Selection,source.Stages)));
                return RequiredSet(CosmicProjectedSetInput<Holon,CosmicProjectionRecord>.FromEntityProjection(entitySelection));
            }
            var selected=RequiredSet(captured.SelectColumns(columns));
            if(source.Stages!=null)selected=ApplyCompositionStages(selected,source.Stages);
            return RequiredSet(CosmicProjectedSetInput<Holon,CosmicProjectionRecord>.FromSource(selected));
        }
        CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> Node(CqlSetV1Node node)
        {
            CosmicProjectedSetInput<Holon,CosmicProjectionRecord> Input(CqlSetV1Source source)
            {
                if(source.Set==null)return Leaf(source);
                if(source.Columns==null)return RequiredSet(CosmicProjectedSetInput<Holon,CosmicProjectionRecord>.FromSet(Node(source.Set)));
                if(source.Columns.Length is <1 or >32)throw new ArgumentException("Select one to32 columns after identity membership.");
                var identity=CompileEntitySetNode(source.Set,actor,provider,access);
                var selection=RequiredSet(identity.SelectColumns(source.Columns.Select(ProjectionColumn).ToArray()));
                if(source.Stages!=null)selection=RequiredSet(selection.WithSelectedPipeline(ApplyCompositionStages(selection.Selection,source.Stages)));
                return RequiredSet(CosmicProjectedSetInput<Holon,CosmicProjectionRecord>.FromIdentityProjection(selection));
            }
            var query=RequiredSet(CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.CreateTree(
                Input(node.Left),Input(node.Right),ParseNativeSetKind(node.Kind)));
            return ApplyProjectedSetNodeOutput(query,node);
        }
        return Node(RootSetNode(request,true));
    }
    private static CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> ApplyProjectedSetNodeOutput(
        CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> query,CqlSetV1Node node)
    {
        if(node.Ordering!=null)
        {
            if(node.Ordering.Length is <1 or >4)throw new ArgumentException("Selected set ordering requires one to four aliases.");
            var aliases=new HashSet<string>(StringComparer.Ordinal);
            foreach(var term in node.Ordering)
            {
                if(term?.Field==null || !aliases.Add(term.Field))
                    throw new ArgumentException("Selected set ordering aliases must be nonempty and unique.");
                var position=query.Output.OutputAliases.IndexOf(term.Field);
                if(position<0)throw new ArgumentException("Selected set ordering must name an exact visible alias.");
                var column=query.Output.Columns[query.Output.OutputColumnIndices[position]];
                query=BindSelectedOrdering(column.ValueType,term.Field,term.Descending,aliases.Count>1,
                    new ProjectedSetOrderingConsumer(query));
            }
        }
        if(node.Reverse)query=RequiredSet(query.Reverse());
        if(node.Skip!=0)query=RequiredSet(query.Skip(node.Skip));
        if(node.Take.HasValue)query=RequiredSet(query.Take(node.Take.Value));
        if(node.SelectedStages!=null)query=RequiredSet(query.WithSelectedOutput(ApplyCompositionStages(query.Output,node.SelectedStages)));
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
