using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public class CosmosDbClient : ICosmosDbClient
    {
        private readonly Container _container;

        public CosmosDbClient(Container container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public async Task<string> ReadDocumentAsync(string documentId, PartitionKey? partitionKey, CancellationToken cancellationToken = default)
        {
            var pk = partitionKey ?? PartitionKey.None;
            var response = await _container.ReadItemStreamAsync(documentId, pk, null, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new CosmosException(response.ErrorMessage ?? "Read failed", response.StatusCode, (int)response.StatusCode, response.Headers.ActivityId, response.Headers.RequestCharge);
            using var reader = new StreamReader(response.Content);
            return await reader.ReadToEndAsync();
        }

        public string ReadDocumentByField(string fieldName, string fieldValue, int version = 0)
        {
            var query = new QueryDefinition($"SELECT * FROM c WHERE c.{fieldName} = @value")
                .WithParameter("@value", fieldValue);
            if (version > 0)
                query = new QueryDefinition($"SELECT * FROM c WHERE c.{fieldName} = @value AND c.version = @version")
                    .WithParameter("@value", fieldValue)
                    .WithParameter("@version", version);

            using var iterator = _container.GetItemQueryStreamIterator(query);
            if (!iterator.HasMoreResults)
                return null;
            var response = iterator.ReadNextAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                throw new CosmosException(response.ErrorMessage ?? "Query failed", response.StatusCode, (int)response.StatusCode, response.Headers.ActivityId, response.Headers.RequestCharge);
            using var reader = new StreamReader(response.Content);
            var content = reader.ReadToEnd();
            var obj = JObject.Parse(content);
            var docs = obj["Documents"] as JArray;
            if (docs == null || !docs.Any())
                return null;
            return docs.First().ToString();
        }

        public List<string> ReadAllDocuments()
        {
            var list = new List<string>();
            var query = new QueryDefinition("SELECT * FROM c");
            using var iterator = _container.GetItemQueryStreamIterator(query);
            while (iterator.HasMoreResults)
            {
                var response = iterator.ReadNextAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                    throw new CosmosException(response.ErrorMessage ?? "Query failed", response.StatusCode, (int)response.StatusCode, response.Headers.ActivityId, response.Headers.RequestCharge);
                using var reader = new StreamReader(response.Content);
                var content = reader.ReadToEnd();
                var obj = JObject.Parse(content);
                var docs = obj["Documents"] as JArray;
                if (docs != null)
                {
                    foreach (var doc in docs)
                        list.Add(doc.ToString());
                }
            }
            return list;
        }

        public async Task<string> CreateDocumentAsync(object document, PartitionKey? partitionKey, CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(document)));
            var pk = partitionKey ?? PartitionKey.None;
            var response = await _container.CreateItemStreamAsync(stream, pk, null, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new CosmosException(response.ErrorMessage ?? "Create failed", response.StatusCode, (int)response.StatusCode, response.Headers.ActivityId, response.Headers.RequestCharge);
            using var reader = new StreamReader(response.Content);
            return await reader.ReadToEndAsync();
        }

        public async Task ReplaceDocumentAsync(string documentId, object document, PartitionKey? partitionKey, CancellationToken cancellationToken = default)
        {
            var pk = partitionKey ?? PartitionKey.None;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(document)));
            var response = await _container.ReplaceItemStreamAsync(stream, documentId, pk, null, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new CosmosException(response.ErrorMessage ?? "Replace failed", response.StatusCode, (int)response.StatusCode, response.Headers.ActivityId, response.Headers.RequestCharge);
        }

        public async Task DeleteDocumentAsync(string documentId, PartitionKey? partitionKey, CancellationToken cancellationToken = default)
        {
            var pk = partitionKey ?? PartitionKey.None;
            await _container.DeleteItemAsync<object>(documentId, pk, null, cancellationToken);
        }
    }
}
