using System;
using System.IO;
using System.Data;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Google.Cloud.Storage.V1;
using Google.Cloud.Firestore;
using Google.Cloud.BigQuery.V2;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS
{
    public partial class GoogleCloudOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private StorageClient _storageClient;
        private FirestoreDb _firestoreDb;
        private BigQueryClient _bigQueryClient;
        private readonly string _projectId;
        private readonly string _bucketName;
        private readonly string _credentialsPath;
        private readonly string _firestoreDatabaseId;
        private readonly string _bigQueryDatasetId;
        private readonly bool _enableStorage;
        private readonly bool _enableFirestore;
        private readonly bool _enableBigQuery;

        public GoogleCloudOASIS(string projectId = null, string bucketName = null, string credentialsPath = null, 
                               string firestoreDatabaseId = null, string bigQueryDatasetId = null,
                               bool enableStorage = true, bool enableFirestore = true, bool enableBigQuery = true)
        {
            this.ProviderName = "GoogleCloudOASIS";
            this.ProviderDescription = "GoogleCloudOASIS Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.GoogleCloudOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            
            _projectId = projectId ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") ?? "oasis-project";
            _bucketName = bucketName ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_BUCKET") ?? "oasis-storage";
            _credentialsPath = credentialsPath;
            _firestoreDatabaseId = firestoreDatabaseId ?? "(default)";
            _bigQueryDatasetId = bigQueryDatasetId ?? "oasis_data";
            _enableStorage = enableStorage;
            _enableFirestore = enableFirestore;
            _enableBigQuery = enableBigQuery;
        }
    }
}
