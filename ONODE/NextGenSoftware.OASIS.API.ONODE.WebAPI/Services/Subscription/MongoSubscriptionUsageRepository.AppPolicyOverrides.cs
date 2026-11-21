using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed class AppPolicyOverrides
{
    public string AppId { get; set; }
    public bool? AccessLogEnabled { get; set; }
    public bool? SealedHistoryEnabled { get; set; }
}

public sealed partial class MongoSubscriptionUsageRepository
{
    /// <summary>Complete policy input, projected on Mongo; no admin-page cap or credential/billing fields.</summary>
    public async Task<IReadOnlyList<AppPolicyOverrides>> ListAppPolicyOverridesAsync(CancellationToken cancellationToken = default)
        => await AppAccounts.Find(_ => true)
            .Project(account => new AppPolicyOverrides
            {
                AppId = account.AppId,
                AccessLogEnabled = account.Overrides.AccessLogEnabled,
                SealedHistoryEnabled = account.Overrides.SealedHistoryEnabled
            }).ToListAsync(cancellationToken).ConfigureAwait(false);
}
