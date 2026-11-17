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
    public static CosmicJoinSelection<Holon,Holon,CosmicProjectionRecord,CosmicProjectionRecord> CompileJoin(
        CqlJoinV1Request request,Guid actor,ProviderType? provider)
        =>CompileJoin(request,actor,provider,null);

    public static CosmicJoinSelection<Holon,Holon,CosmicProjectionRecord,CosmicProjectionRecord> CompileJoin(
        CqlJoinV1Request request,Guid actor,ProviderType? provider,CosmicQueryReadAccess access)
    {
        if(request==null || request.Left==null || request.Right==null || request.Keys==null || request.Keys.Length >8
            || request.Columns==null || request.Columns.Length is <1 or >32 || request.MaximumLeftRows is <1 or >1000
            || request.MaximumRightRows is <1 or >1000 || request.MaximumPairs is <1 or >10000 || request.MaximumResults is <1 or >100)
            throw new ArgumentException("Join sources, up to eight keys, one to 32 columns and bounded input, pair and output limits are required.");
        var children = request.Kind == "Child";
        var kind=request.Kind switch {"Inner" or "Child"=>CosmicJoinKind.Inner,"Left"=>CosmicJoinKind.Left,
            "Cross"=>CosmicJoinKind.Cross,"Full"=>CosmicJoinKind.Full,
            _=>throw new ArgumentException("Join kind must be Inner, Left, Full, Cross or Child.")};
        if ((children || kind==CosmicJoinKind.Cross) != (request.Keys.Length==0))
            throw new ArgumentException("Cross and Child joins accept no explicit keys. Other joins require at least one key.");
        CosmicQuery<Holon> Source(CqlJoinV1Source source)=>CompileSource(actor,provider,source.Filters,source.AnyOf,
            source.AnyChildren,source.AnyDescendants,source.Graph,source.OrderBy,source.Descending,source.Skip,source.Take,access);
        var left=Source(request.Left);var right=Source(request.Right);
        var leftKeys=request.Keys.Select(key=>
        {
            if(key==null)throw new ArgumentException("Join keys cannot be null.");
            return ProjectionColumn(JoinColumn(key.Alias,key.Left));
        }).ToArray();
        var rightKeys=request.Keys.Select(key=>ProjectionColumn(JoinColumn(key.Alias,key.Right))).ToArray();
        var options=RequiredJoin(CosmicJoinOptions.Create(request.MaximumLeftRows,request.MaximumRightRows,request.MaximumPairs,
            request.MaximumResults,1024*1024,1024*1024,1024*1024));
        var captured=children ? left.JoinChildColumns(right,options) : kind switch
        {
            CosmicJoinKind.Cross=>left.CrossJoin(right,options),
            CosmicJoinKind.Left=>left.LeftJoinColumns(right,leftKeys,rightKeys,options),
            CosmicJoinKind.Full=>left.FullJoinColumns(right,leftKeys,rightKeys,options),
            _=>left.JoinColumns(right,leftKeys,rightKeys,options)
        };
        var join=RequiredJoin(captured);
        if (request.KeyGroupStarts != null)
        {
            if (request.MatchAnyKey) throw new ArgumentException("Specify either grouped keys or MatchAnyKey.");
            join = RequiredJoin(join.MatchKeyGroups(request.KeyGroupStarts));
        }
        else if (request.MatchAnyKey) join = RequiredJoin(join.MatchAnyKey());
        foreach (var key in request.Keys)
        {
            var comparison = key.Operator switch
            {
                null or "Equal" => CosmicJoinComparison.Equal,
                "NotEqual" => CosmicJoinComparison.NotEqual,
                "IsDistinctFrom" => CosmicJoinComparison.NullSafeNotEqual,
                "IsNotDistinctFrom" => CosmicJoinComparison.NullSafeEqual,
                "LessThan" => CosmicJoinComparison.LessThan,
                "LessThanOrEqual" => CosmicJoinComparison.LessThanOrEqual,
                "GreaterThan" => CosmicJoinComparison.GreaterThan,
                "GreaterThanOrEqual" => CosmicJoinComparison.GreaterThanOrEqual,
                _ => throw new ArgumentException("Unknown join key operator.")
            };
            if (comparison != CosmicJoinComparison.Equal)
                join = RequiredJoin(join.CompareKey(key.Alias, comparison));
        }
        var columns=request.Columns.Select(column=>
        {
            if(column==null)throw new ArgumentException("Join output columns cannot be null.");
            ValidateSelectionAlias(column.Alias);
            if(column.Source=="RightPresence")
            {
                if(column.Selection!=null || column.AllowMissing.HasValue)
                    throw new ArgumentException("Presence does not select a path or allow a missing scalar.");
                return RequiredJoin(CosmicJoinBinding.Presence(join,column.Alias,column.WhenMissing??false));
            }
            if(column.Source is not ("Left" or "Right") || column.WhenMissing.HasValue
                || column.Source=="Left" && kind != CosmicJoinKind.Full && column.AllowMissing.HasValue)
                throw new ArgumentException("Select Left,Right or RightPresence with the corresponding options.");
            return CompileSelection(JoinColumn(column.Alias,column.Selection),
                new JoinSelectionConsumer(join,column.Source=="Right",column.AllowMissing??false));
        }).ToArray();
        var selected = RequiredJoin(join.SelectColumns(columns));
        if (request.OutputOrdering is { } terms)
        {
            if (request.OutputOrderBy != null || request.OutputDescending || terms.Length is < 1 or > 8)
                throw new ArgumentException("Provide one to eight join ordering terms or a single output alias.");
            for (var index = 0; index < terms.Length; index++)
            {
                var term = terms[index] ?? throw new ArgumentException("Join ordering terms cannot be null.");
                selected = RequiredJoin(index == 0
                    ? selected.OrderByOutput(term.Alias, term.Descending)
                    : selected.ThenByOutput(term.Alias, term.Descending));
            }
        }
        else if (request.OutputOrderBy != null)
            selected = RequiredJoin(selected.OrderByOutput(request.OutputOrderBy,request.OutputDescending));
        else if (request.OutputDescending)
            throw new ArgumentException("A descending join order requires an output alias.");

        if (request.OutputLimit is int limit)
            return RequiredJoin(selected.PageOutput(request.OutputOffset ?? 0,limit));
        if (request.OutputOffset.HasValue)
            throw new ArgumentException("A join output offset requires an output limit.");
        return selected;
    }

    private static CqlProjectionV1Column JoinColumn(string alias,CqlScalarV1Selection selection)
    {
        if(selection==null)throw new ArgumentException("An explicit typed join path is required.");
        return new CqlProjectionV1Column{Alias=alias,Field=selection.Field,MetadataPath=selection.MetadataPath,
            ValueType=selection.ValueType,Nullable=selection.Nullable};
    }

    private sealed class JoinSelectionConsumer : ISelectionConsumer<CosmicJoinBinding<Holon,Holon,CosmicProjectionRecord>>
    {
        private readonly CosmicJoinQuery<Holon,Holon,CosmicProjectionRecord> owner;
        private readonly bool right,allowMissing;
        internal JoinSelectionConsumer(CosmicJoinQuery<Holon,Holon,CosmicProjectionRecord> owner,bool right,bool allowMissing)
        {this.owner=owner;this.right=right;this.allowMissing=allowMissing;}
        public CosmicJoinBinding<Holon,Holon,CosmicProjectionRecord> Bind<TValue>(string alias,Expression<Func<Holon,TValue>> selector)
            =>RequiredJoin(right?CosmicJoinBinding.Right(owner,alias,selector,allowMissing):CosmicJoinBinding.Left(owner,alias,selector,allowMissing));
    }

    private static T RequiredJoin<T>(OASISResult<T> result)
    {
        if(result.IsError)throw new ArgumentException(result.Message);
        return result.Result;
    }
}
