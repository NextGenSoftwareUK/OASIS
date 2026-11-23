using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private static string CompileKeyedSourceSql<T>(CqlSqlTranslator translator,CosmicQuery<T> query,
        IReadOnlyList<CosmicQueryPath> keyPaths,IReadOnlyList<CosmicQueryPath> valuePaths,
        int maximumRows,string predicate,string prefix) where T:IHolon
        {
            var ordering = translator.TranslateOrdering(query.Orderings);
            var cells = new List<string>
            {
                "row_number() OVER (ORDER BY " + ordering + ") AS position",
                "\"Holon\".\"Id\" AS id", "\"Holon\".\"IsPublic\" AS public",
                "\"Holon\".\"CreatedByAvatarId\" AS creator"
            };
            for (var index = 0; index < keyPaths.Count; index++)
                Cell(keyPaths[index], "k" + index);
            for (var index = 0; index < valuePaths.Count; index++) Cell(valuePaths[index], "v" + index);
            for (var index = 0; index < query.Orderings.Count; index++) Cell(query.Orderings[index].Path, "o" + index);
            var take = translator.Bind(Math.Min(query.Limit.HasValue ? (long)query.Limit.Value : long.MaxValue, (long)maximumRows + 1));
            var offset = translator.Bind((long)query.Offset);
            return "SELECT " + string.Join(",", cells) + " FROM Holon WHERE " + predicate
                + " ORDER BY " + ordering + " LIMIT " + take + " OFFSET " + offset;

            void Cell(CosmicQueryPath path, string alias)
            {
                var operand = translator.ProjectionOperand(path, prefix + alias, comparable: true);
                cells.Add(operand.State + " AS " + alias + "s");
                cells.Add(operand.Value + " AS " + alias + "v");
            }
        }
}
