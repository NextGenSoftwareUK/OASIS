using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.AWSOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static partial class CloudProviderFactory
{
    // Existing official DynamoDB SDK adapter. No activation or implicit account provisioning.
    internal static AWSOASIS CreateAws(AWSOASISProviderSettings settings,string overrideConnectionString)
    {
        if(settings==null) throw new ArgumentNullException(nameof(settings));
        if(string.IsNullOrWhiteSpace(settings.Region) || string.IsNullOrWhiteSpace(settings.AccessKey) || string.IsNullOrWhiteSpace(settings.SecretKey))
            throw new ArgumentException("AWS DynamoDB requires an explicit region and signing credentials.",nameof(settings));
        return new AWSOASIS(settings.Region,settings.AccessKey,settings.SecretKey,overrideConnectionString??settings.ServiceUrl);
    }
}
