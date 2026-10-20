using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public interface ICosmosDbClient
    {
        Task<string> ReadDocumentAsync(string documentId, PartitionKey? partitionKey, CancellationToken cancellationToken = default);
        string ReadDocumentByField(string fieldName, string fieldValue, int version = 0);
        List<string> ReadAllDocuments();
        Task<List<string>> ReadAllDocumentsAsync();
        Task<string> CreateDocumentAsync(object document, PartitionKey? partitionKey, CancellationToken cancellationToken = default);
        Task<string> UpsertDocumentAsync(object document, PartitionKey? partitionKey, CancellationToken cancellationToken = default);
        Task<string> UpsertPlannedDocumentAsync(object document, NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite plan, PartitionKey? partitionKey, CancellationToken cancellationToken = default);
        Task<string> ReplaceDocumentAsync(string documentId, object document, PartitionKey? partitionKey, CancellationToken cancellationToken = default);
        Task<string> ReplaceUnchangedDocumentAsync(string documentId, object expected, object document, PartitionKey? partitionKey, CancellationToken cancellationToken = default);
        Task DeleteDocumentAsync(string documentId, PartitionKey? partitionKey, CancellationToken cancellationToken = default);
        Task DeleteUnchangedDocumentAsync(string documentId, object expected, PartitionKey? partitionKey, CancellationToken cancellationToken = default);
    }
}
