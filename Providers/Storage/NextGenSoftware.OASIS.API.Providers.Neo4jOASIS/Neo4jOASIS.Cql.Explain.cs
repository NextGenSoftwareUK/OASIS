using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicQueryExplainProvider
{
    /// <summary>The exact read-only Cypher a plan compiles to; nothing is executed.</summary>
    public async Task<OASISResult<CosmicQueryExplanation>> ExplainCosmicQueryAsync<T>(CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        CosmicQueryExplanation explanation = null;
        var compiled = await ExecuteCqlReadAsync(plan, maximumResults, cancellationToken,
            explain: (text, parameters) => explanation = new CosmicQueryExplanation("Cypher", text, parameters.Keys.OrderBy(key => key, System.StringComparer.Ordinal))).ConfigureAwait(false);
        if (compiled.IsError) return new OASISResult<CosmicQueryExplanation> { IsError = true, ErrorCount = 1, ErrorCode = compiled.ErrorCode, Message = compiled.Message, Exception = compiled.Exception };
        return explanation == null
            ? new OASISResult<CosmicQueryExplanation> { IsError = true, ErrorCount = 1, ErrorCode = "CQL_EXPLAIN_UNAVAILABLE", Message = "The plan produced no native command." }
            : new OASISResult<CosmicQueryExplanation>(explanation);
    }
}
