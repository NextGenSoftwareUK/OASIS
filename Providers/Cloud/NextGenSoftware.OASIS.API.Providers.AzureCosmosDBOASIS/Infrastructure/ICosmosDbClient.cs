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
        Task<string> CreateDocumentAsync(object document, PartitionKey? partitionKey, CancellationToken cancellationToken = default);
        Task ReplaceDocumentAsync(string documentId, object document, PartitionKey? partitionKey, CancellationToken cancellationToken = default);
        Task DeleteDocumentAsync(string documentId, PartitionKey? partitionKey, CancellationToken cancellationToken = default);
    }
}
