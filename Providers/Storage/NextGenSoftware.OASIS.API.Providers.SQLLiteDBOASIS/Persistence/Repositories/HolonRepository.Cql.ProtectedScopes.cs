using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        // Scalar-only source predicates delimit eligible roots. Within each
        // eligible root, protected scopes run independently of Boolean truth.
        internal string TranslateSource(IReadOnlyList<CosmicQueryPredicate> predicates)
        {
            bool Relationship(CosmicQueryPredicate node)
                => CosmicScalarCapabilities.HasAllChildren(new[] { node })
                    || CosmicScalarCapabilities.HasAllDescendants(new[] { node });
            var scalar = predicates.Where(node => !Relationship(node)).Select(Translate).ToArray();
            var relationships = predicates.Where(Relationship).ToArray();
            var eligible = scalar.Length == 0 ? "1" : string.Join(" AND ", scalar);
            if (relationships.Length == 0) return eligible;
            var matched = string.Join(" AND ", relationships.Select(Translate));
            return "(" + eligible + ") AND (" + ProtectTraversalScope(relationships,
                "CASE WHEN COALESCE((" + matched + "),0)<>0 THEN 1 ELSE 0 END") + ")";
        }

        internal string ProtectTraversalScope(IEnumerable<CosmicQueryPredicate> predicates, string value)
        {
            var pending = new Stack<CosmicQueryPredicate>(predicates);
            while (pending.Count != 0)
            {
                var node = pending.Pop();
                if (node is CosmicQueryAnyDescendant || node is CosmicQueryAnyChild)
                    value = "CASE WHEN (" + Translate(node) + ") IN (0,1) THEN (" + value + ") ELSE 0 END";
                else if (node is CosmicQueryLogical logical) { pending.Push(logical.Left); pending.Push(logical.Right); }
                else if (node is CosmicQueryNot not) pending.Push(not.Operand);
                // A protected child scope is observed as one expression here.
                // Its descendants remain bound to that child, never this root;
                // even an outer true OR / false AND cannot bypass its errors.
            }
            return value;
        }
    }
}
