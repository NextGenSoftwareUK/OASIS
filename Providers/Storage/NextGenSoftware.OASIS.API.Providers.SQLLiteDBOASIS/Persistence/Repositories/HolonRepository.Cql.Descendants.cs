using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        internal bool HasTraversal { get; private set; }
        internal bool HasRelationship { get; private set; }

        private static string MembershipObservationSql(CqlSqlTranslator parent)
        {
            var payload = parent.CanonicalColumn("PersistentValueJson");
            var manifest = "json_extract(" + payload + ",'$.ChildRoleManifestJson')";
            return "(CASE WHEN " + payload + " IS NOT NULL AND NOT json_valid(" + payload + ") THEN "
                + SqliteCqlTraversalGuard.FunctionName + "(3) WHEN " + manifest + " IS NULL THEN 1 WHEN "
                + SqliteCqlTraversalGuard.ValidationFunctionName + "(" + manifest + "," + parent.CanonicalColumn("Id") + ")=0 THEN 0 ELSE 1 END)";
        }

        private static string MembershipSql(CqlSqlTranslator parent, CqlSqlTranslator child)
        {
            var manifest = "json_extract(" + parent.CanonicalColumn("PersistentValueJson") + ",'$.ChildRoleManifestJson')";
            return "(CASE WHEN " + MembershipObservationSql(parent) + "=0 THEN 0 WHEN " + manifest + " IS NULL THEN 1 "
                + "ELSE EXISTS (SELECT 1 FROM json_each("
                + manifest + ",'$.roles') AS role, json_each(role.value,'$.childIds') AS member "
                + "WHERE member.type='text' AND member.value=lower(" + child.CanonicalColumn("Id") + ")) END)";
        }

        private string VisibilitySql(CqlSqlTranslator child)
        {
            if (_access == null) return "1";
            var secured = CosmicQuery<Core.Holons.Holon>.Create().WithReadAccess(_access);
            if (secured.IsError) throw new System.NotSupportedException(secured.Message);
            return child.Translate(secured.Result.Predicates.Single());
        }

        private string TranslateDescendant(CosmicQueryAnyDescendant descendant)
        {
            HasTraversal = true;
            HasRelationship = true;
            var suffix = Parameters.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var walk = "w" + suffix;
            var parent = new CqlSqlTranslator("p" + suffix, Parameters, _access);
            var child = new CqlSqlTranslator("d" + suffix, Parameters, _access);
            var rootId = "lower(" + CanonicalColumn("Id") + ")";
            var childId = "lower(" + child.CanonicalColumn("Id") + ")";
            var childParent = "lower(" + child.CanonicalColumn("ParentHolonId") + ")";
            var maximumDepth = Bind(descendant.Traversal.MaximumDepth);
            var maximumNodes = Bind(descendant.Traversal.MaximumNodes);
            var fetch = Bind(descendant.Traversal.MaximumNodes + 1);
            var visibility = VisibilitySql(child);
            var predicate = child.Translate(descendant.Predicate);
            return "(CASE WHEN " + MembershipObservationSql(this) + "=0 THEN 0 ELSE (WITH RECURSIVE " + walk + "(id,depth,path,cycle) AS ("
                + "SELECT " + child.CanonicalColumn("Id") + ",1,'|'||" + rootId + "||'|'||" + childId + "||'|'," + childId + "=" + rootId
                + " FROM Holon AS \"" + child._table + "\" WHERE " + childParent + "=" + rootId
                + " AND " + MembershipSql(this, child) + " AND (" + visibility + ") UNION ALL "
                + "SELECT " + child.CanonicalColumn("Id") + "," + walk + ".depth+1," + walk + ".path||" + childId + "||'|',"
                + "instr(" + walk + ".path,'|'||" + childId + "||'|')>0 FROM " + walk
                + " JOIN Holon AS \"" + parent._table + "\" ON lower(" + parent.CanonicalColumn("Id") + ")=lower(" + walk + ".id)"
                + " JOIN Holon AS \"" + child._table + "\" ON " + childParent + "=lower(" + parent.CanonicalColumn("Id") + ")"
                + " WHERE " + walk + ".cycle=0 AND " + walk + ".depth<" + maximumDepth
                + " AND " + MembershipSql(parent, child) + " AND (" + visibility + ") LIMIT " + fetch + ") "
                + "SELECT CASE WHEN coalesce(max(cycle),0)>0 THEN " + SqliteCqlTraversalGuard.FunctionName + "(1)"
                + " WHEN count(*)>" + maximumNodes + " THEN " + SqliteCqlTraversalGuard.FunctionName + "(2)"
                + " ELSE EXISTS(SELECT 1 FROM " + walk + " JOIN Holon AS \"" + child._table + "\" ON "
                + child.CanonicalColumn("Id") + "=" + walk + ".id WHERE (" + predicate + ")) END FROM " + walk + ") END)";
        }
    }
}
