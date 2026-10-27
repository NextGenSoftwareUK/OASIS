using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        internal string ProtectTraversalScope(IEnumerable<CosmicQueryPredicate> predicates, string value)
        {
            var pending = new Stack<CosmicQueryPredicate>(predicates);
            while (pending.Count != 0)
            {
                var node = pending.Pop();
                if (node is CosmicQueryAnyDescendant || node is CosmicQueryAnyChild child
                    && CosmicScalarCapabilities.HasAllDescendants(new[] { child.Predicate }))
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
