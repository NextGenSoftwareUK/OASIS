using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicQueryStorageProvider
{
    Core.Enums.ProviderType ICosmicQueryStorageProvider.QueryProviderType => Core.Enums.ProviderType.SQLLiteDBOASIS;
    CosmicQueryCapabilities ICosmicQueryStorageProvider.QueryCapabilities => CqlCapabilities;

    /// <summary>Exact immutable native predicate contract; it grants no caller authorization.</summary>
    public static CosmicQueryCapabilities CqlCapabilities => SqliteCqlContract.Capabilities;

    /// <summary>Native leaf-row execution of an admitted SQLite-only plan. WEB4 authorization is a separate required boundary.</summary>
    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        => ((HolonRepository)_holonRepository).ExecuteCosmicQueryAsync(plan, maximumResults, cancellationToken);
}
