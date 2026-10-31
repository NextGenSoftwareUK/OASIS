using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

public partial class CosmosDbClient
{
    // Native row transport only. CQL admission, visibility and SQL translation must
    // finish before calling this owner; this does not enroll any CQL capability.
    internal async Task<IReadOnlyList<string>> ReadBoundedQueryAsync(QueryDefinition command,
        int maximumRows, int maximumPages, int maximumResponseBytes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateQueryBounds(maximumRows, maximumPages, maximumResponseBytes);
        cancellationToken.ThrowIfCancellationRequested();
        using var iterator = _container.GetItemQueryStreamIterator(command,
            requestOptions: new QueryRequestOptions { MaxItemCount = Math.Min(maximumRows, 100) });
        return await ReadBoundedPagesAsync(iterator, maximumRows, maximumPages,
            maximumResponseBytes, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<IReadOnlyList<string>> ReadBoundedPagesAsync(FeedIterator iterator,
        int maximumRows, int maximumPages, int maximumResponseBytes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(iterator);
        ValidateQueryBounds(maximumRows, maximumPages, maximumResponseBytes);
        var rows = new List<string>();
        var pages = 0;
        var bytes = 0;
        var buffer = new byte[8192];

        while (iterator.HasMoreResults)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (++pages > maximumPages)
                throw new InvalidDataException("Cosmos CQL response page budget exceeded.");

            using var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw CreateServiceException(response, "Cosmos CQL query failed.");
            if (response.Content == null)
                throw new InvalidDataException("Cosmos CQL query response lacks content.");

            using var body = new MemoryStream();
            int count;
            while ((count = await response.Content.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) != 0)
            {
                if (count > maximumResponseBytes - bytes)
                    throw new InvalidDataException("Cosmos CQL response byte budget exceeded.");
                bytes += count;
                body.Write(buffer, 0, count);
            }

            body.Position = 0;
            using var reader = new StreamReader(body, System.Text.Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var documents = DecodeQueryDocuments(await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false));
            if (documents.Count > maximumRows - rows.Count)
                throw new InvalidDataException("Cosmos CQL response row budget exceeded.");
            foreach (var document in documents)
                rows.Add(document.ToString(Formatting.None));
        }

        cancellationToken.ThrowIfCancellationRequested();
        // Publish only complete results. No partial success on a later page error.
        return rows.AsReadOnly();
    }

    private static void ValidateQueryBounds(int rows, int pages, int bytes)
    {
        if (rows < 1 || rows > 10000) throw new ArgumentOutOfRangeException(nameof(rows));
        if (pages < 1 || pages > 1000) throw new ArgumentOutOfRangeException(nameof(pages));
        if (bytes < 1 || bytes > 32 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(bytes));
    }
}
