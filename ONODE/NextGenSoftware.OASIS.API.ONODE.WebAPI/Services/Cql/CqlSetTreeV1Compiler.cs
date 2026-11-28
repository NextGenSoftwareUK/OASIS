using System;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
public static partial class CqlV1Compiler
{
    private static CosmicSetQueryKind ParseNativeSetKind(string kind)=>kind switch {
        "Union"=>CosmicSetQueryKind.Union,"Concat"=>CosmicSetQueryKind.Concat,
        "Intersect"=>CosmicSetQueryKind.Intersect,"Except"=>CosmicSetQueryKind.Except,
        _=>throw new ArgumentException("Unknown native set operation.")};
    private static CqlSetV1Node RootSetNode(CqlSetV1Request request,bool selected)=>new() {
        Kind=request.Kind,Left=request.Left,Right=request.Right,Skip=request.Skip,Take=request.Take,
        Ordering=request.Ordering,Reverse=request.Reverse,EntityStages=request.EntityStages,
        SelectedStages=selected?request.SelectedStages:null};
    /// <summary>One bounded discriminator for endpoint routing and compilation. It never reads a provider.</summary>
    public static bool UsesSelectedSet(CqlSetV1Request request)
    {
        if(request==null)throw new ArgumentException("A captured set request is required.");
        var leaves=0;
        bool Source(CqlSetV1Source source,int depth)
        {
            if(source==null||source.Graph!=null||depth>CosmicSetQuery<Holon>.MaximumTreeDepth)
                throw new ArgumentException("A bounded set source without graph expansion is required.");
            if(source.Set!=null)
            {
                if(source.Where!=null||source.Filters?.Length is >0||source.AnyOf?.Length is >0
                    ||source.AnyChildren?.Length is >0||source.AnyDescendants?.Length is >0
                    ||source.Columns!=null||source.Stages!=null||source.OrderBy!="Name"||source.Descending
                    ||source.Skip!=0||source.Take.HasValue)
                    throw new ArgumentException("A nested set operand cannot also contain leaf source settings; put its stages inside Set.");
                return Node(source.Set,depth+1);
            }
            if(++leaves>CosmicSetQuery<Holon>.MaximumTreeLeaves)
                throw new ArgumentException("The set request exceeds its leaf bound.");
            if(source.Columns==null&&source.Stages!=null)
                throw new ArgumentException("Selected input stages require explicit selected columns.");
            return source.Columns!=null;
        }
        bool Node(CqlSetV1Node node,int depth)
        {
            if(node==null||depth>CosmicSetQuery<Holon>.MaximumTreeDepth||node.Skip is <0 or >10000||node.Take is <0 or >100)
                throw new ArgumentException("A bounded native sequence node is required.");
            _=ParseNativeSetKind(node.Kind);
            var left=Source(node.Left,depth);var right=Source(node.Right,depth);
            if(left!=right)throw new ArgumentException("Entity identity and selected-value set operands cannot mix.");
            if(left&&node.EntityStages!=null||!left&&node.SelectedStages!=null)
                throw new ArgumentException("A sequence node must use stages matching its entity or selected-value inputs.");
            return left;
        }
        var selectedLeft=Source(request.Left,1);var selectedRight=Source(request.Right,1);
        if(selectedLeft!=selectedRight)throw new ArgumentException("Entity and selected-value set operands cannot mix.");
        return selectedLeft;
    }
}
