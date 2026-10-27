using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    // Every stage consumes the preceding native relation. Position is carried
    // explicitly: SQLite subquery order alone cannot preserve LINQ windows.
    private sealed class EntityCompositionSql
    {
        internal readonly List<SqliteParameter> Parameters;
        internal readonly string Rows;
        internal readonly string Source;
        internal readonly IReadOnlyList<string> StageCounts;
        internal readonly IReadOnlyList<string> ProtectedScopes;
        internal readonly CqlSqlTranslator SourceTranslator;
        internal readonly string SourcePredicate;
        internal readonly bool HasRelationship;
        internal EntityCompositionSql(List<SqliteParameter> parameters, string rows, string source,
            List<string> counts, List<string> scopes, CqlSqlTranslator translator, string predicate, bool relationships)
        {
            Parameters = parameters; Rows = rows; Source = source; StageCounts = counts;
            ProtectedScopes = scopes; SourceTranslator = translator; SourcePredicate = predicate;
            HasRelationship = relationships;
        }
    }

    private static EntityCompositionSql CompileEntityCompositionSql<T>(CosmicEntityCompositionPlan<T> plan)
        where T : IHolon
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));
        var source = plan.Query.Source;
        var translator = new CqlSqlTranslator(source.ReadAccess);
        var predicate = translator.TranslateSource(source.Predicates);
        var ordering = translator.TranslateOrdering(source.Orderings);
        var definitions = new List<string>();
        var counts = new List<string>();
        var scopes = new List<string>();
        var relation = "entity0";
        var position = "__cosmicEntityPosition0";
        definitions.Add(relation + " AS MATERIALIZED (SELECT Holon.*,row_number() OVER (ORDER BY "
            + ordering + ") AS " + position + " FROM Holon WHERE " + predicate + " ORDER BY " + ordering
            + " LIMIT " + translator.Bind(source.Limit ?? -1) + " OFFSET " + translator.Bind(source.Offset) + ")");
        string Prefix() => "WITH " + string.Join(",", definitions) + " ";
        var sourceSql = Prefix() + "SELECT * FROM " + relation + " ORDER BY " + position;
        var hasRelationship = translator.HasRelationship;
        foreach (var stage in plan.Query.Stages)
        {
            // Observe the whole current relation before a later empty window can
            // suppress protected traversal errors or the cumulative-work bound.
            counts.Add(Prefix() + "SELECT count(*) AS Value FROM " + relation);
            var next = "entity" + definitions.Count;
            string select;
            string protectedScope = null;
            if (stage is CosmicEntityFilterStage<T> filter)
            {
                var filterTranslator = new CqlSqlTranslator("Holon", translator.Parameters, source.ReadAccess);
                var test = string.Join(" AND ", filter.Predicate.Predicates.Select(filterTranslator.Translate));
                if (filterTranslator.HasRelationship)
                {
                    var observed = filterTranslator.ProtectTraversalScope(filter.Predicate.Predicates,
                        "CASE WHEN COALESCE((" + test + "),0)<>0 THEN 1 ELSE 0 END");
                    protectedScope = Prefix() + "SELECT COALESCE(sum(" + observed + "),0) AS Value FROM " + relation + " AS Holon";
                    hasRelationship = true;
                }
                select = "SELECT * FROM " + relation + " AS Holon WHERE COALESCE((" + test + "),0)<>0";
            }
            else if (stage is CosmicEntityOrderStage order)
            {
                var stageOrdering = translator.TranslateOrdering(order.Orderings);
                position = "__cosmicEntityPosition" + definitions.Count;
                select = "SELECT Holon.*,row_number() OVER (ORDER BY " + stageOrdering + ") AS "
                    + position + " FROM " + relation + " AS Holon";
            }
            else if (stage is CosmicEntityReverseStage)
            {
                var priorPosition=position;
                position="__cosmicEntityPosition" + definitions.Count;
                select="SELECT Holon.*,row_number() OVER (ORDER BY " + priorPosition + " DESC) AS "
                    + position + " FROM " + relation + " AS Holon";
            }
            else if (stage is CosmicEntityWindowStage window)
            {
                select = "SELECT * FROM " + relation + " ORDER BY " + position + " LIMIT "
                    + translator.Bind(window.Kind == CosmicSelectedWindowKind.Take ? window.Count : -1)
                    + " OFFSET " + translator.Bind(window.Kind == CosmicSelectedWindowKind.Skip ? window.Count : 0);
            }
            else throw new NotSupportedException("Unknown native entity stage.");
            scopes.Add(protectedScope);
            definitions.Add(next + " AS MATERIALIZED (" + select + ")");
            relation = next;
        }
        return new EntityCompositionSql(translator.Parameters,
            Prefix() + "SELECT * FROM " + relation + " ORDER BY " + position,
            sourceSql, counts, scopes, translator, predicate, hasRelationship);
    }
}
