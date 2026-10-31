using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static partial class CloudProviderFactory
{
    internal static GoogleCloudOASIS CreateGoogle(GoogleCloudOASISProviderSettings settings)
    {
        if(settings==null) throw new ArgumentNullException(nameof(settings));
        return new GoogleCloudOASIS(settings.ProjectId,settings.BucketName,settings.CredentialsPath,
            settings.FirestoreDatabaseId,settings.BigQueryDatasetId,settings.EnableStorage,settings.EnableFirestore,settings.EnableBigQuery)
            {IsVersionControlEnabled=settings.IsVersionControlEnabled};
    }
}
