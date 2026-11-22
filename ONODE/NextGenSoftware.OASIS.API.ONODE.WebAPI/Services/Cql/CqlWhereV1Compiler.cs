using System;
using System.Linq;
using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Compiler
{
    // Validate the entire description before allocating expression nodes.
    private static void ValidateWhere(CqlWhereV1Predicate root, ref int filters, ref int relationships)
    {
        int nodes = 0, leaves = 0, related = 0;
        void Visit(CqlWhereV1Predicate node, int depth)
        {
            if (node == null || depth > 8 || ++nodes > 31)
                throw new ArgumentException("WHERE accepts at most31nodes and8levels.");
            bool simple = node.Filter == null && node.Match == null &&
                node.MaximumDepth == null && node.MaximumNodes == null;
            switch (node.Kind)
            {
                case "Filter":
                    if (node.Filter == null || node.Terms != null || node.Match != null ||
                        node.MaximumDepth != null || node.MaximumNodes != null)
                        throw new ArgumentException("Filter requires exactly one bounded filter.");
                    leaves++;
                    break;
                case "And":
                case "Or":
                case "Not":
                    if (!simple || node.Terms == null ||
                        (node.Kind == "Not" ? node.Terms.Length != 1 : node.Terms.Length is < 2 or > 12))
                        throw new ArgumentException("Boolean nodes require canonical operands only.");
                    foreach (var term in node.Terms) Visit(term, depth + 1);
                    break;
                case "Child":
                case "Descendant":
                    if (node.Filter != null || node.Terms != null ||
                        node.Kind == "Child" && (node.MaximumDepth != null || node.MaximumNodes != null))
                        throw new ArgumentException("Relationship nodes require only a match and supported traversal bounds.");
                    if (++related > 4) throw new ArgumentException("Use at most four relationships.");
                    if (node.Kind == "Descendant")
                    {
                        var limits = CosmicQueryTraversalOptions.Create(node.MaximumDepth ?? 8, node.MaximumNodes ?? 1000);
                        if (limits.IsError) throw new ArgumentException(limits.Message);
                    }
                    if (node.Match != null) Visit(node.Match, depth + 1);
                    break;
                default: throw new ArgumentException("Unknown WHERE node kind.");
            }
        }
        Visit(root, 1);
        if (filters + leaves > 12 || relationships + related > 4)
            throw new ArgumentException("WHERE and flat predicates share twelve filters and four relationships.");
        filters += leaves;
        relationships += related;
    }

    private static Expression WhereExpression(CqlWhereV1Predicate node, ParameterExpression row)
    {
        if (node.Kind == "Filter") return Predicate(node.Filter, row);
        if (node.Kind == "Not") return Expression.Not(WhereExpression(node.Terms[0], row));
        if (node.Kind is "And" or "Or")
        {
            var expression = WhereExpression(node.Terms[0], row);
            foreach (var term in node.Terms.Skip(1))
                expression = node.Kind == "And"
                    ? Expression.AndAlso(expression, WhereExpression(term, row))
                    : Expression.OrElse(expression, WhereExpression(term, row));
            return expression;
        }
        var child = Expression.Parameter(typeof(IHolon), "related");
        var match = Expression.Lambda<Func<IHolon, bool>>(node.Match == null
            ? Expression.Constant(true) : WhereExpression(node.Match, child), child);
        if (node.Kind == "Child")
        {
            var children = Expression.Property(row, typeof(ISemanticHolon).GetProperty(nameof(ISemanticHolon.Children)));
            return Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new[] { typeof(IHolon) }, children, match);
        }
        var options = CosmicQueryTraversalOptions.Create(node.MaximumDepth ?? 8, node.MaximumNodes ?? 1000);
        return Expression.Call(typeof(CosmicQueryRelations), nameof(CosmicQueryRelations.AnyDescendant), null,
            row, match, Expression.Constant(options.Result));
    }
}
