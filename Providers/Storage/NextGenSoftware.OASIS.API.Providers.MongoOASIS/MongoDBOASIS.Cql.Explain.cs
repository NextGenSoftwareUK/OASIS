using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICosmicQueryExplainProvider
{
    /// <summary>The exact aggregation pipeline a plan compiles to (Q66); nothing is executed. MongoDB has
    /// no bound parameters, so literal values appear inside the pipeline and ParameterNames is empty.</summary>
    public async Task<OASISResult<CosmicQueryExplanation>> ExplainCosmicQueryAsync<T>(CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        MongoCqlCommand command = null;
        var compiled = await ExecuteCqlRowsAsync(plan, maximumResults, cancellationToken, explain: native => command = native).ConfigureAwait(false);
        if (compiled.IsError) return new OASISResult<CosmicQueryExplanation> { IsError = true, ErrorCount = 1, ErrorCode = compiled.ErrorCode, Message = compiled.Message, Exception = compiled.Exception };
        if (command == null)
            return new OASISResult<CosmicQueryExplanation> { IsError = true, ErrorCount = 1, ErrorCode = "CQL_EXPLAIN_UNAVAILABLE", Message = "The plan produced no native command." };
        return new OASISResult<CosmicQueryExplanation>(new CosmicQueryExplanation("MongoDB aggregation pipeline",
            new BsonArray(command.Pipeline ?? Array.Empty<BsonDocument>()).ToJson(), Enumerable.Empty<string>()));
    }
}
