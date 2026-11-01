using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

/// <summary>Bind shared bounded identity selection to the two independently
/// named Dynamo/AWS authorities. Native execution uses BatchGetItem, never Scan.</summary>
public static class DynamoCqlCompiler
{
    public static CosmicQueryCapabilities Capabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(
        CosmicQuery<T> query, ProviderType authority) where T : IHolon
    {
        if (authority is not (ProviderType.AWSOASIS or ProviderType.DynamoDBOASIS))
            return new()
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "CQL_ACCESS_INVALID",
                Message = "Only an explicit DynamoDB or AWS authority is supported."
            };

        // One compiler owns predicate capture, catalogue intersection and bounds.
        // Changing an enum must not grant another provider access to this reader.
        return BoundedIdentityCqlCompiler.Compile(query, authority, authority);
    }
}
