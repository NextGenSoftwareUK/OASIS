using System;
using System.Globalization;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed partial class NeoCqlTranslator
{
    // Q47: a correlated pattern comprehension over the root's direct children, scoped exactly like child existence
    // (membership manifest, projection revision and caller visibility), reduced natively. Empty scopes give 0.
    private string TranslateChildAggregate(CosmicQueryChildAggregate aggregate)
    {
        var suffix = _relationshipDepth.ToString(CultureInfo.InvariantCulture) + "_" + Parameters.Count.ToString(CultureInfo.InvariantCulture);
        var node = "ah" + suffix; var projection = "ap" + suffix;
        var inner = new NeoCqlTranslator(node, projection, Parameters, _access, _relationshipDepth + 1);
        var visibility = "true";
        if (_access != null)
        {
            var secured = CosmicQuery<Core.Holons.Holon>.Create().WithReadAccess(_access);
            if (secured.IsError) throw new NotSupportedException(secured.Message);
            visibility = inner.Translate(secured.Result.Predicates.Single());
        }
        var match = inner.Translate(aggregate.Predicate);
        string item;
        if (aggregate.Function == CosmicQueryChildAggregateFunction.Count) item = "1";
        else
        {
            var operand = inner.Operand(aggregate.Path);
            item = "CASE WHEN coalesce((" + operand.Guard + "),false) THEN " + operand.Value + " ELSE 0 END";
        }
        var values = "[(" + _node + ")-[:HAS_CHILD]->(" + node + ":Holon)-[" + projection + ":HAS_CQL_PROJECTION]->(" + node + ") WHERE "
            + ChildScopeCondition(node, projection, visibility) + " AND (" + match + ") | " + item + "]";
        var reduced = "reduce(cqlTotal" + suffix + " = 0, cqlItem" + suffix + " IN " + values + " | cqlTotal" + suffix + " + cqlItem" + suffix + ")";
        return Total(reduced + Symbol(aggregate.Operator) + Bind(aggregate.Value));
    }
}
