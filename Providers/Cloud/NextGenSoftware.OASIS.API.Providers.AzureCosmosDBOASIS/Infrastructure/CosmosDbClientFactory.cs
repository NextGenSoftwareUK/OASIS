using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public class CosmosDbClientFactory : ICosmosDbClientFactory
    {
        private readonly string _databaseName;
        private readonly List<string> _collectionNames;
        private readonly IDocumentClient _documentClient;

        public CosmosDbClientFactory(string databaseName, List<string> collectionNames, IDocumentClient documentClient)
        {
            _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            _collectionNames = collectionNames ?? throw new ArgumentNullException(nameof(collectionNames));
            _documentClient = documentClient ?? throw new ArgumentNullException(nameof(documentClient));
        }

        public ICosmosDbClient GetClient(string collectionName)
        {
            if (!_collectionNames.Contains(collectionName))
            {
                throw new ArgumentException($"Unable to find collection: {collectionName}");
            }

            return new CosmosDbClient(_databaseName, collectionName, _documentClient);
        }

        public async Task<OASISResult<bool>> EnsureDbSetupAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                await _documentClient.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(_databaseName));

                foreach (var collectionName in _collectionNames)
                {
                    DocumentCollection collection = await _documentClient.ReadDocumentCollectionAsync(
                        UriFactory.CreateDocumentCollectionUri(_databaseName, collectionName));

                    if (collection == null)
                    {
                        result.Message = $"Error occured in EnsureDbSetupAsync method in AzureCosmosDBOASIS Provider. Reason: collection {collection} is null.";
                        result.IsError = true;
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateProviderAsync method in AzureCosmosDBOASIS Provider. Reason: {ex}");
            }

            result.Result = true;
            return result;
        }
    }
}
