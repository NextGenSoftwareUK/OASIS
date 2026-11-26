using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        // Q47: a correlated scalar SELECT over the root's direct children, with the same membership and caller
        // visibility scope as child existence. count(*) and coalesce(sum(..),0) make empty scopes 0.
        private string TranslateChildAggregate(CosmicQueryChildAggregate aggregate)
        {
            HasRelationship = true;
            var alias = "a" + _relationshipDepth.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_"
                + Parameters.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var inner = new CqlSqlTranslator(alias, Parameters, _access, _relationshipDepth + 1);
            var parent = CanonicalColumn("Id");
            var scope = " FROM Holon AS \"" + alias + "\" WHERE lower(" + inner.CanonicalColumn("ParentHolonId")
                + ")=lower(" + parent + ") AND lower(" + inner.CanonicalColumn("Id") + ")<>lower(" + parent + ") AND "
                + MembershipSql(this, inner) + " AND (" + VisibilitySql(inner) + ")";
            var childMatch = inner.Translate(aggregate.Predicate);
            string value;
            if (aggregate.Function == CosmicQueryChildAggregateFunction.Count) value = "count(*)";
            else
            {
                var path = aggregate.Path;
                var item = inner.MetadataSubquery(path.Segments, column => MetadataColumn(column, path.ValueType),
                    column => MetadataTypeGuard(path.ValueType, column), alias + "m");
                value = "coalesce(sum((" + item + ")),0)";
            }
            return "(CASE WHEN " + MembershipObservationSql(this) + "=0 THEN 0 ELSE COALESCE(((SELECT " + value + scope
                + " AND (" + childMatch + "))" + ComparisonSymbol(aggregate.Operator) + Bind(aggregate.Value) + "),0) END)";
        }
    }
}
