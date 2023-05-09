using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public interface ICosmosDbClient
    {
        public IQueryable<Document> ReadAllDocumentsAsync();
        Task<Document> ReadDocumentAsync(string documentId, RequestOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken));

        public Task<Document> CreateDocumentAsync(object document, RequestOptions options = null,
            bool disableAutomaticIdGeneration = false,
            CancellationToken cancellationToken = default(CancellationToken));

        public Task<Document> ReplaceDocumentAsync(string documentId, object document, RequestOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken));

        public Task<Document> DeleteDocumentAsync(string documentId, RequestOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}