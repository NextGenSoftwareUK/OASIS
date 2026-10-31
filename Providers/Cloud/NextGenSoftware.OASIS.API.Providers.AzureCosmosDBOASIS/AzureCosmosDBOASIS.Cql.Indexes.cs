using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public partial class AzureCosmosDBOASIS
{
    /// <summary>
    /// Detached deployment definitions for the canonical CQL Version/Id profile.
    /// The caller explicitly merges these into its container policy; this method
    /// and CQL execution perform no index mutation. Reverse scans cover the
    /// opposite directions. Preserve existing paths, indexes and policy settings.
    /// </summary>
    public static IReadOnlyList<Collection<CompositePath>> GetCqlCompositeIndexDefinitions()
        => new[]
        {
            new Collection<CompositePath>
            {
                new() { Path = "/Version", Order = CompositePathSortOrder.Ascending },
                new() { Path = "/Id", Order = CompositePathSortOrder.Ascending }
            },
            new Collection<CompositePath>
            {
                new() { Path = "/Version", Order = CompositePathSortOrder.Descending },
                new() { Path = "/Id", Order = CompositePathSortOrder.Ascending }
            }
        };
}
