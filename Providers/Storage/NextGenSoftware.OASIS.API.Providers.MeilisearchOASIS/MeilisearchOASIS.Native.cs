using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
    private async Task SubmitTaskAsync(HttpMethod method, string path, object payload,
        string index, string type, bool documentWrite = false)
    {
        long? taskId = null;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            using var request = new HttpRequestMessage(method, _host + path) { Content = Json(payload) };
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
                    if (documentWrite)
                    {
                        var details = task.GetProperty("details");
                        if (details.GetProperty("receivedDocuments").GetInt32() != 1 ||
                            details.GetProperty("indexedDocuments").GetInt32() != 1)
                            throw new InvalidDataException("Native task did not index the submitted document.");
                    }
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

    private Task UpsertDocAsync(string index, object document)
        // POST replaces the full document; PUT would retain fields removed by the caller.
        => SubmitTaskAsync(HttpMethod.Post, $"/indexes/{index}/documents", new[] { document },
            index, "documentAdditionOrUpdate", documentWrite: true);
}
