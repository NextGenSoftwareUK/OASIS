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
        if(string.IsNullOrWhiteSpace(settings.Region)) throw new ArgumentException("AWS DynamoDB requires an explicit region.",nameof(settings));
        if(settings.UseDefaultCredentials)
        {
            if(!string.IsNullOrEmpty(settings.AccessKey) || !string.IsNullOrEmpty(settings.SecretKey) || !string.IsNullOrEmpty(settings.SessionToken))
                throw new ArgumentException("Choose SDK default credentials or explicit credentials, not both.",nameof(settings));
        }
        else if(string.IsNullOrWhiteSpace(settings.AccessKey) || string.IsNullOrWhiteSpace(settings.SecretKey))
            throw new ArgumentException("AWS DynamoDB requires explicit signing credentials unless SDK default credentials are opted in.",nameof(settings));
        return new AWSOASIS(settings.Region,settings.AccessKey,settings.SecretKey,overrideConnectionString??settings.ServiceUrl,
            settings.SessionToken,settings.UseDefaultCredentials) {IsVersionControlEnabled=settings.IsVersionControlEnabled};
    }
}
