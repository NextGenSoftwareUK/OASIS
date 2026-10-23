using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        internal string TranslateProjection(IReadOnlyList<CosmicProjectionColumn> columns)
        {
            var sql = new List<string>();
            for (var index = 0; index < columns.Count; index++)
            {
                var path = columns[index].Path;
                var prefix = "cqlproject" + index;
                string state, value;
                if (path.IsMetadata)
                {
                    // Preserve presence/type separately. A wrong domain is never
                    // projected as NULL, and ambiguous duplicate paths are errors.
                    var count = "(" + MetadataSubquery(path.Segments, _ => "count(*)", _ => "1", prefix + "c") + ")";
                    var kind = "(" + MetadataSubquery(path.Segments, alias =>
                        "CASE WHEN " + alias + ".type='null' THEN 1 WHEN " + MetadataTypeGuard(path.ValueType, alias)
                        + " THEN 2 ELSE 3 END", _ => "1", prefix + "s") + " LIMIT 1)";
                    state = "CASE WHEN " + count + ">1 THEN 3 ELSE COALESCE(" + kind + ",0) END";
                    value = "(" + MetadataSubquery(path.Segments, alias => alias + ".value", _ => "1", prefix + "v") + " LIMIT 1)";
                }
                else
                {
                    string native = null;
                    FieldOperand(path, prefix, (column, _) => { native = column; return "1"; });
                    value = native ?? throw new NotSupportedException("Canonical projection field is unsupported.");
                    state = "CASE WHEN " + value + " IS NULL THEN 1 ELSE 2 END";
                }
                // SQL aliases are compiler-owned ordinal names, never DTO names.
                sql.Add(state + " AS p" + index + "s");
                sql.Add(value + " AS p" + index + "v");
            }
            return string.Join(", ", sql);
        }
    }
}
