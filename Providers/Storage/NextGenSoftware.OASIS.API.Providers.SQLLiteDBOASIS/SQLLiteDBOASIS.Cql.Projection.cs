using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicQueryEntityProjectionStorageProvider,ICosmicQuerySetProjectionStorageProvider
{
    public static CosmicProjectionCapabilities CqlProjectionCapabilities { get; } = new CosmicProjectionCapabilities(
        SqliteCqlContract.Fields.Values.Select(field => new CosmicProjectionFieldCapability(field.Name, field.ValueType)).ToArray(),
        SqliteCqlContract.MetadataTypes.Keys.ToArray()).WithFunctions(CosmicProjectionFunction.Utf16Length,CosmicProjectionFunction.TextCoalesce,CosmicProjectionFunction.BooleanTextCase,CosmicProjectionFunction.Int32Coalesce,CosmicProjectionFunction.UtcYear,CosmicProjectionFunction.UtcMonth,CosmicProjectionFunction.UtcDay,CosmicProjectionFunction.UtcDayOfYear,CosmicProjectionFunction.UtcHour,CosmicProjectionFunction.UtcMinute,CosmicProjectionFunction.UtcSecond,CosmicProjectionFunction.TextValueCase,CosmicProjectionFunction.Int32Affine,CosmicProjectionFunction.TextToInt32Case);
    public CosmicProjectionCapabilities ProjectionCapabilities => CqlProjectionCapabilities;

    public bool SupportsEntityProjection => true;
    public bool SupportsSetProjection => true;

    public Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicSetProjectionAsync<T,TResult>(
        CosmicSetProjectionPlan<T,TResult> plan,int maximumResults=100,
        int maximumPayloadBytes=CosmicProjectionResults.MaximumPayloadLimit,CancellationToken cancellationToken=default) where T:IHolon
        =>!IsProviderActivated || _holonRepository is not HolonRepository repository
            ?Task.FromResult(new OASISResult<IReadOnlyList<CosmicProjectionRow>>{IsError=true,ErrorCount=1,
                ErrorCode="CQL_PROVIDER_NOT_READY",Message="Activate SQLite before late set projection."})
            :repository.ExecuteCosmicSetProjectionAsync(plan,maximumResults,maximumPayloadBytes,cancellationToken);


    public Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicEntityProjectionAsync<T,TResult>(
        CosmicEntityProjectionPlan<T,TResult> plan,int maximumResults=100,
        int maximumPayloadBytes=CosmicProjectionResults.MaximumPayloadLimit,CancellationToken cancellationToken=default) where T:IHolon
        => ((HolonRepository)_holonRepository).ExecuteCosmicEntityProjectionAsync(plan,maximumResults,maximumPayloadBytes,cancellationToken);

    public Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicProjectionAsync<T, TResult>(
        CosmicProjectionPlan<T, TResult> plan, int maximumResults = 100,
        int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken = default) where T : IHolon
        => ((HolonRepository)_holonRepository).ExecuteCosmicProjectionAsync(plan, maximumResults, maximumPayloadBytes, cancellationToken);
}
