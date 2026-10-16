using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Common;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public class CosmosDbClient : ICosmosDbClient
    {
        private readonly Container _container;

        private static CosmosException CreateServiceException(ResponseMessage response,string message)
        {
            var header=response.Headers.Get("x-ms-substatus");
            var substatus=int.TryParse(header,NumberStyles.Integer,CultureInfo.InvariantCulture,out var value) ? value:0;
            return new CosmosException(response.ErrorMessage ?? message,response.StatusCode,substatus,response.Headers.ActivityId,response.Headers.RequestCharge);
        }

        public CosmosDbClient(Container container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        private static async Task<ResponseMessage> ObserveMutationAsync(Func<Task<ResponseMessage>> operation,string id)
        {
            ResponseMessage response=null;
            try
            {
                response=await operation();
                if(response.IsSuccessStatusCode) return response;
                throw CreateServiceException(response,"Mutation failed");
            }
            catch(Exception cause)
            {
                response?.Dispose();
                if(cause is CosmosException rejection && (int)rejection.StatusCode is 400 or 401 or 403 or 404 or 409 or 412 or 413 or 429) throw;
                throw new CosmosMutationReconciliationException(id,cause);
            }
        }

        public async Task<string> ReadDocumentAsync(string documentId, PartitionKey? partitionKey, CancellationToken cancellationToken = default)
        {
            var pk = partitionKey ?? PartitionKey.None;
            using var response = await _container.ReadItemStreamAsync(documentId, pk, null, cancellationToken);
            var substatus=response.Headers.Get("x-ms-substatus");
            if(response.StatusCode==System.Net.HttpStatusCode.NotFound && (string.IsNullOrEmpty(substatus) || substatus=="0"))
            {
                // Item absence is authoritative only while the parent container remains readable.
                await _container.ReadContainerAsync(cancellationToken:cancellationToken);
                return null;
            }
            if (!response.IsSuccessStatusCode)
                throw CreateServiceException(response,"Read failed");
            using var reader = new StreamReader(response.Content);
            return await reader.ReadToEndAsync();
        }

        public string ReadDocumentByField(string fieldName, string fieldValue, int version = 0)
        {
            if(string.IsNullOrWhiteSpace(fieldName) || !Regex.IsMatch(fieldName,@"\A[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*\z"))
                throw new ArgumentException("A valid Cosmos property path is required.",nameof(fieldName));
            var query = new QueryDefinition($"SELECT * FROM c WHERE c.{fieldName} = @value")
                .WithParameter("@value", fieldValue);
            if (version > 0)
                query = new QueryDefinition($"SELECT * FROM c WHERE c.{fieldName} = @value AND c.version = @version")
                    .WithParameter("@value", fieldValue)
                    .WithParameter("@version", version);

            using var iterator = _container.GetItemQueryStreamIterator(query);
            string match=null;
            while(iterator.HasMoreResults)
            {
                using var response=iterator.ReadNextAsync().GetAwaiter().GetResult();
                if(!response.IsSuccessStatusCode)
                    throw CreateServiceException(response,"Query failed");
                using var reader=new StreamReader(response.Content);
                var docs=DecodeQueryDocuments(reader.ReadToEnd());
                foreach(var doc in docs)
                {
                    if(match!=null) throw new StorageIdentityAmbiguousException();
                    match=doc.ToString();
                }
            }
            return match;
        }

        public List<string> ReadAllDocuments()
        {
            var list = new List<string>();
            var query = new QueryDefinition("SELECT * FROM c");
            using var iterator = _container.GetItemQueryStreamIterator(query);
            while (iterator.HasMoreResults)
            {
                using var response = iterator.ReadNextAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                    throw CreateServiceException(response,"Query failed");
                using var reader = new StreamReader(response.Content);
                var content = reader.ReadToEnd();
                foreach (var doc in DecodeQueryDocuments(content))
                    list.Add(doc.ToString());
            }
            return list;
        }

        private static JArray DecodeQueryDocuments(string content)
        {
            try
            {
                var obj=JObject.Parse(content);
                if(obj["Documents"] is not JArray docs) throw new InvalidDataException("Cosmos query response lacks a Documents array.");
                if(docs.Any(doc=>doc is not JObject)) throw new InvalidDataException("Cosmos query response contains a non-object document.");
                return docs;
            }
            catch(JsonException cause)
            {
                throw new InvalidDataException("Cosmos query response JSON is invalid.",cause);
            }
        }

        public async Task<string> CreateDocumentAsync(object document, PartitionKey? partitionKey, CancellationToken cancellationToken = default)
        {
            var jsonDocument = JObject.Parse(OasisJson.Serialize(document));
            jsonDocument["id"] = jsonDocument["Id"]?.ToString();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonDocument.ToString(Formatting.None)));
            var pk = partitionKey ?? PartitionKey.None;
            cancellationToken.ThrowIfCancellationRequested();
            using var response = await ObserveMutationAsync(()=>_container.CreateItemStreamAsync(stream, pk, null, cancellationToken),jsonDocument["id"].ToString());
            if (!response.IsSuccessStatusCode)
                throw CreateServiceException(response,"Create failed");
            using var reader = new StreamReader(response.Content);
            return await reader.ReadToEndAsync();
        }

        public async Task<string> UpsertDocumentAsync(object document, PartitionKey? partitionKey, CancellationToken cancellationToken = default)
        {
            var jsonDocument = JObject.Parse(OasisJson.Serialize(document));
            jsonDocument["id"] = jsonDocument["Id"]?.ToString();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonDocument.ToString(Formatting.None)));
            cancellationToken.ThrowIfCancellationRequested();
            using var response = await ObserveMutationAsync(()=>_container.UpsertItemStreamAsync(stream, partitionKey ?? PartitionKey.None, null, cancellationToken),jsonDocument["id"].ToString());
            if (!response.IsSuccessStatusCode)
                throw CreateServiceException(response,"Upsert failed");
            using var reader = new StreamReader(response.Content);
            return await reader.ReadToEndAsync();
        }

        public async Task ReplaceDocumentAsync(string documentId, object document, PartitionKey? partitionKey, CancellationToken cancellationToken = default)
        {
            var pk = partitionKey ?? PartitionKey.None;
            var jsonDocument = JObject.Parse(OasisJson.Serialize(document));
            jsonDocument["id"] = documentId;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonDocument.ToString(Formatting.None)));
            cancellationToken.ThrowIfCancellationRequested();
            using var response = await ObserveMutationAsync(()=>_container.ReplaceItemStreamAsync(stream, documentId, pk, null, cancellationToken),documentId);
            if (!response.IsSuccessStatusCode)
                throw CreateServiceException(response,"Replace failed");
        }

        public async Task DeleteDocumentAsync(string documentId, PartitionKey? partitionKey, CancellationToken cancellationToken = default)
        {
            var pk = partitionKey ?? PartitionKey.None;
            cancellationToken.ThrowIfCancellationRequested();
            using var response=await ObserveMutationAsync(()=>_container.DeleteItemStreamAsync(documentId,pk,null,cancellationToken),documentId);
        }
    }
}
