using System;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AppwriteOASIS
{
    /// <summary>
    /// OASIS provider for Appwrite (cloud or self-hosted) over the Databases REST API.
    ///
    /// Avatars, avatar details, holons and their indexes are stored by the shared
    /// <see cref="KeyValueStorageProviderBase"/> as one Appwrite document per key in a single collection.
    ///
    /// Pre-requisites (see the integration fixture script for an automated example):
    ///   1. A Database (databaseId) and a Collection (collectionId) in it with document security off.
    ///   2. Collection attributes: key (string, size 255, required) and value (string, large size, required),
    ///      plus a key index on "key" for prefix listing.
    ///   3. An API key with documents.read, documents.write and collections.read scopes.
    /// </summary>
    public class AppwriteOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        /// <param name="appwriteUrl">API endpoint, e.g. "https://cloud.appwrite.io/v1" or "http://127.0.0.1/v1".</param>
        /// <param name="projectId">Appwrite project ID.</param>
        /// <param name="apiKey">Appwrite API key with document scopes.</param>
        /// <param name="databaseId">Database containing the OASIS collection.</param>
        /// <param name="collectionId">Collection holding one document per OASIS key.</param>
        public AppwriteOASIS(string appwriteUrl, string projectId, string apiKey, string databaseId, string collectionId = "oasis")
            : this(appwriteUrl, projectId, apiKey, databaseId, collectionId, new HttpClient()) { }

        public AppwriteOASIS(string appwriteUrl, string projectId, string apiKey, string databaseId, string collectionId, HttpClient httpClient)
            : base(new AppwriteDocumentBackend(appwriteUrl, projectId, apiKey, databaseId, collectionId, httpClient))
        {
            ProviderName = "AppwriteOASIS";
            ProviderDescription = "Appwrite provider: OASIS avatars and holons as Appwrite documents (Databases REST API).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AppwriteOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }
    }
}
