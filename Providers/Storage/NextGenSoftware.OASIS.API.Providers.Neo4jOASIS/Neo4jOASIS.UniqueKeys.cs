using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicUniqueKeyStorageProvider
{
    public async Task<OASISResult<Guid>> FindUniqueKeyHolderAsync(string scope, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = new CosmicUniqueKeyClaim(Guid.NewGuid(), scope, value).NativeKey;
            await using var session = _driver.AsyncSession();
            var rows = await (await session.RunAsync("MATCH (k:CosmicUniqueKey {KeyId:$key}) RETURN k.HolonId AS holder", new { key })
                .ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            return new OASISResult<Guid>(rows.Count == 0 ? Guid.Empty : Guid.Parse(rows[0]["holder"].As<string>())) { IsLoaded = true };
        }
        catch (Exception cause)
        {
            return new OASISResult<Guid> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_UNIQUE_KEY_LOOKUP_FAILED", Message = "The Neo4j unique key could not be read.", Exception = cause };
        }
    }
}
