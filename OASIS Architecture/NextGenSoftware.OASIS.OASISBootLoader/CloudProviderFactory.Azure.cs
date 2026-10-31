using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static partial class CloudProviderFactory
{
    internal static AzureStorageOASIS CreateAzureStorage(AzureStorageOASISProviderSettings settings,string overrideConnectionString)
    {
        if(settings==null) throw new ArgumentNullException(nameof(settings));
        var connection=overrideConnectionString??settings.ConnectionString;
        if(string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(settings.HolonContainer)
            || string.IsNullOrWhiteSpace(settings.AvatarContainer) || string.IsNullOrWhiteSpace(settings.AvatarDetailContainer))
            throw new ArgumentException("Azure Blob Storage requires an explicit connection and three named containers.",nameof(settings));
        return new AzureStorageOASIS(connection,settings.HolonContainer,settings.AvatarContainer,settings.AvatarDetailContainer)
            {IsVersionControlEnabled=settings.IsVersionControlEnabled};
    }
}
