using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS;

public partial class MeilisearchOASIS
{
    private sealed class NativePayloadLimitException : IOException { }

    private static async Task<JsonDocument> ReadNativeResponseAsync(HttpResponseMessage response,
        int maximumBytes, CancellationToken cancellation)
    {
        using var source = await response.Content.ReadAsStreamAsync(cancellation);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await source.ReadAsync(buffer, cancellation)) != 0)
        {
            if (body.Length + count > maximumBytes) throw new NativePayloadLimitException();
            body.Write(buffer, 0, count);
        }
        return JsonDocument.Parse(body.ToArray());
    }

    // One submission, followed only by observations of that exact native task.
    // A deadline/transport failure after submission has an unknown write outcome;
    // the task ID is retained in the exception and must never trigger resubmission.
    private async Task SubmitTaskAsync(HttpMethod method, string path, object? payload,
        string index, string type, Action<JsonElement>? validateDetails = null)
    {
        long? taskId = null;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            using var request = new HttpRequestMessage(method, _host + path);
            if (payload != null)
                request.Content = Json(payload);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (response.StatusCode != HttpStatusCode.Accepted)
                throw new InvalidDataException("Native operation did not return an accepted task.");
            using var acknowledgement = await ReadNativeResponseAsync(response, 65536, deadline.Token);
            var receipt = acknowledgement.RootElement;
            var id = receipt.GetProperty("taskUid").GetInt64();
            if (id < 0) throw new InvalidDataException("Invalid native task identity.");
            taskId = id;
            ValidateTask(receipt, index, type);
            if (receipt.GetProperty("status").GetString() != "enqueued")
                throw new InvalidDataException("Unexpected native task acknowledgement.");
            while (true)
            {
                using var observation = new HttpRequestMessage(HttpMethod.Get, $"{_host}/tasks/{id}");
                using var observed = await _http.SendAsync(observation, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                observed.EnsureSuccessStatusCode();
                using var state = await ReadNativeResponseAsync(observed, 65536, deadline.Token);
                var task = state.RootElement;
                if (task.GetProperty("uid").GetInt64() != id)
                    throw new InvalidDataException("Native task observation changed identity.");
                ValidateTask(task, index, type);
                var status = task.GetProperty("status").GetString();
                if (status == "succeeded")
                {
                    if (task.GetProperty("error").ValueKind != JsonValueKind.Null)
                        throw new InvalidDataException("Native task reported an error with success.");
                    validateDetails?.Invoke(task.GetProperty("details"));
                    return;
                }
                if (status != "enqueued" && status != "processing")
                    throw new InvalidDataException("Native task did not succeed: " + status);
                await Task.Delay(100, deadline.Token);
            }
        }
        catch (Exception cause)
        {
            throw new InvalidOperationException(taskId.HasValue
                ? $"Native task {taskId.Value} was not acknowledged successful; reconcile that task before another write."
                : "Native submission was not acknowledged successful; reconcile the original outcome before another write.", cause);
        }
    }

    private static void ValidateTask(JsonElement task, string index, string type)
    {
        if (task.GetProperty("indexUid").GetString() != index || task.GetProperty("type").GetString() != type)
            throw new InvalidDataException("Native task differs from submitted index/operation.");
    }

    private async Task EnsureIndexAsync(string index)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_host}/indexes/{index}");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            using var error = await ReadNativeResponseAsync(response, 65536, deadline.Token);
            if (error.RootElement.GetProperty("code").GetString() != "index_not_found")
                throw new InvalidDataException("Index absence was not confirmed.");
            await SubmitTaskAsync(HttpMethod.Post, "/indexes", new { uid = index, primaryKey = "id" }, index, "indexCreation");
            // Verify the acknowledged schema, rather than assuming creation selected our key.
            using var verification = new HttpRequestMessage(HttpMethod.Get, $"{_host}/indexes/{index}");
            using var verified = await _http.SendAsync(verification, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            verified.EnsureSuccessStatusCode();
            using var schema = await ReadNativeResponseAsync(verified, 65536, deadline.Token);
            ValidateIndex(schema.RootElement, index);
            return;
        }
        response.EnsureSuccessStatusCode();
        using var existing = await ReadNativeResponseAsync(response, 65536, deadline.Token);
        ValidateIndex(existing.RootElement, index);
    }

    private static void ValidateIndex(JsonElement schema, string index)
    {
        if (schema.GetProperty("uid").GetString() != index || schema.GetProperty("primaryKey").GetString() != "id")
            throw new InvalidDataException("Existing Meilisearch index does not match the storage contract.");
    }

    private async Task EnsureIdentityFiltersAsync(string index)
    {
        var path = $"/indexes/{index}/settings/filterable-attributes";
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var response = await _http.GetAsync(_host + path, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        response.EnsureSuccessStatusCode();
        using var settings = await ReadNativeResponseAsync(response, 65536, deadline.Token);
        var existing = new List<JsonElement>();
        var attributes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in settings.RootElement.EnumerateArray())
        {
            existing.Add(item.Clone());
            if (item.ValueKind == JsonValueKind.String)
                attributes.Add(item.GetString()!);
        }
        var required = new List<object>();
        foreach (var field in new[] { "username", "email" })
        {
            if (!attributes.Contains(field))
                required.Add(field);
        }
        if (required.Count == 0)
            return;
        foreach (var item in existing)
            required.Add(item);
        await SubmitTaskAsync(HttpMethod.Put, path, required, index, "settingsUpdate");
    }

    private async Task<OASISResult<TContract>> LoadByIdentityAsync<TContract, TRecord>(string index, string field, string value)
        where TContract : class, IHolonBase
        where TRecord : TContract
    {
        var result = new OASISResult<TContract>();
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            var escaped = value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
            var filter = Uri.EscapeDataString($"{field} = \"{escaped}\"");
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var response = await _http.GetAsync($"{_host}/indexes/{index}/search?filter={filter}&limit=2",
                HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            using var body = await ReadNativeResponseAsync(response, 4 * 1024 * 1024, deadline.Token);
            var hits = body.RootElement.GetProperty("hits");
            if (hits.GetArrayLength() > 1)
                throw new InvalidDataException("Native identity lookup matched multiple documents.");
            TContract? record = hits.GetArrayLength() == 0 ? null : typeof(TRecord) == typeof(AvatarDetail)
                ? StoredRecordJson.DeserializeStored<TRecord>(hits[0].GetRawText())
                : JsonSerializer.Deserialize<TRecord>(hits[0].GetRawText(), _jsonOpts);
            if (record == null || record.IsDeleted)
            {
                OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No live record for {field}.");
                result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                return result;
            }
            result.Result = record;
            result.IsLoaded = true;
            result.LoadedCount = 1;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}", ex);
        }
        return result;
    }

    private Task UpsertDocAsync(string index, object document)
        // POST replaces the full document; PUT would retain fields removed by the caller.
        => SubmitTaskAsync(HttpMethod.Post, $"/indexes/{index}/documents", new[] { document },
            index, "documentAdditionOrUpdate", details =>
            {
                if (details.GetProperty("receivedDocuments").GetInt32() != 1 ||
                    details.GetProperty("indexedDocuments").GetInt32() != 1)
                    throw new InvalidDataException("Native task did not index the submitted document.");
            });

    private Task DeleteDocAsync(string index, string id, bool allowMissing = false)
        => SubmitTaskAsync(HttpMethod.Delete, $"/indexes/{index}/documents/{id}", null,
            index, "documentDeletion", details =>
            {
                var count = details.GetProperty("deletedDocuments").GetInt32();
                if (details.GetProperty("providedIds").GetInt32() != 1 || (count != 1 && !(allowMissing && count == 0)))
                    throw new InvalidDataException("Native task did not confirm the requested document deletion.");
            });
}
