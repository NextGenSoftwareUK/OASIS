using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
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
        internal readonly string Position;
        internal readonly IReadOnlyList<string> StageCounts;
        internal readonly IReadOnlyList<string> ProtectedScopes;
        internal readonly CqlSqlTranslator SourceTranslator;
        internal readonly string SourcePredicate;
        internal readonly bool HasRelationship;
        internal EntityCompositionSql(List<SqliteParameter> parameters, string rows, string source,
            List<string> counts, List<string> scopes, CqlSqlTranslator translator, string predicate, bool relationships,string position)
        {
            Parameters = parameters; Rows = rows; Source = source; StageCounts = counts;
            ProtectedScopes = scopes; SourceTranslator = translator; SourcePredicate = predicate;
            HasRelationship = relationships; Position=position;
        }
    }

    private async Task ValidateEntityCompositionAsync(EntityCompositionSql entitySql,CosmicEntityCompositionOptions options,
        SqliteCqlReadBudget budget,SqliteCqlTraversalGuard traversal,CancellationToken cancellationToken)
    {
        // Source bounds apply before filtering, including a later Take(0).
        // Native rows are measured here, never filtered in managed code.
        var sourceRows = 0;
        long sourceBytes = 0;
        var sourceQuery = _dbContext.Holons.FromSqlRaw(entitySql.Source,
            entitySql.Parameters.Cast<object>().ToArray()).AsNoTracking();
        await foreach (var row in sourceQuery.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            budget.Check(); traversal?.Check();
            sourceBytes = checked(sourceBytes + CqlRecordSize(row));
            if (++sourceRows > options.MaximumInputRows
                || sourceBytes > options.MaximumInputPayloadBytes)
                throw new SqliteCqlResourceException("Entity composition exceeds its native source row/payload budget.");
        }
        long work = 0;
        for (var index = 0; index < entitySql.StageCounts.Count; index++)
        {
            var count = await _dbContext.Database.SqlQueryRaw<long>(entitySql.StageCounts[index],
                entitySql.Parameters.Cast<object>().ToArray()).SingleAsync(cancellationToken).ConfigureAwait(false);
            work = checked(work + count);
            if (count < 0 || work > options.MaximumStageRows)
                throw new SqliteCqlResourceException("Entity composition exceeds its cumulative native stage-work budget.");
            if (entitySql.ProtectedScopes[index] != null)
                await _dbContext.Database.SqlQueryRaw<long>(entitySql.ProtectedScopes[index],
                    entitySql.Parameters.Cast<object>().ToArray()).SingleAsync(cancellationToken).ConfigureAwait(false);
            budget.Check(); traversal?.Check();
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
            sourceSql, counts, scopes, translator, predicate, hasRelationship,position);
    }
}
