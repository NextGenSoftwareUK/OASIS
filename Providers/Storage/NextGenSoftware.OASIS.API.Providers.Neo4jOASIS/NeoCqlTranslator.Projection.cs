using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed partial class NeoCqlTranslator
{
    internal IReadOnlyList<(string State, string Value)> TranslateProjection(IReadOnlyList<CosmicProjectionColumn> columns)
    {
        var projected = new List<(string, string)>();
        foreach (var column in columns)
        {
            var path = column.Path;
            var operand = Operand(path);
            var missing = path.IsMetadata ? _projection + "[" + Bind(NeoCqlProjection.PathKey(path.Segments) + "_kind") + "] IS NULL" : "false";
            var state = "CASE WHEN " + missing + " THEN 0 WHEN " + operand.Null + " THEN 1 WHEN "
                + Total(operand.Guard) + " THEN 2 ELSE 3 END";
            var value = "CASE WHEN " + Total(operand.Guard) + " THEN " + operand.Value + " ELSE null END";
            projected.Add((state, value));
        }
        return projected.AsReadOnly();
    }
}
