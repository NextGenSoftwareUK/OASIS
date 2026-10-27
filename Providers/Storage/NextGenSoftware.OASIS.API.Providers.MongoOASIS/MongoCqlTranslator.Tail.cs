using System.Collections.Generic;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    private static List<BsonDocument> SelectedTailWindow<T>(CosmicQuery<T> query, int oneBasedIndex) where T : IHolon
    {
        // Keep the source Skip/Take intact, then select from its reverse order
        // in the SAME aggregate. No count/read race or client-side row scan.
        var page = SelectedWindow(query, query.Limit);
        var wrapped = new BsonDocument { { "_id", 0 }, { "row", "$$ROOT" } };
        var sort = new BsonDocument();
        for (var index = 0; index < query.Orderings.Count; index++)
        {
            var ordering = query.Orderings[index];
            var key = "s" + index;
            var direction = ordering.Descending ? 1 : -1;
            wrapped.Add(key, OrderingKey(ordering.Path, "cqlTail" + index));
            sort.Add(key + ".rank", direction);
            sort.Add(key + ".value", direction);
        }
        sort.Add("row.HolonId", -1);
        page.Add(new("$project", wrapped));
        page.Add(new("$sort", sort));
        if (oneBasedIndex > 1) page.Add(new("$skip", oneBasedIndex - 1));
        page.Add(new("$limit", 1));
        page.Add(new("$replaceRoot", new BsonDocument("newRoot", "$row")));
        return page;
    }
}
