using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Operator-only typed failure evidence. No raw exception object/text or native values are logged.</summary>
internal static class CqlNativeFailureDiagnostics
{
    internal sealed record Summary(string Categories, string Stage, int? HttpStatus, string WebSocketError);

    internal static Summary Capture(Exception error)
    {
        var categories = new List<string>();
        string stage = "unclassified", socketError = "none";
        int? status = null;
        for (int depth = 0; error != null && depth < 8; depth++, error = error.InnerException)
        {
            string category = error switch
            {
                HttpRequestException => "http", WebSocketException => "websocket",
                NativeConnectionRefusedException => "connection_refused", OperationCanceledException => "cancelled",
                TimeoutException => "timeout",
                InvalidDataException => "invalid_data", JsonException => "invalid_json",
                DecoderFallbackException => "invalid_utf8", _ => "other"
            };
            if (!categories.Contains(category)) categories.Add(category);
            if (error.Data["COSMIC.NativeStage"] is string declared && declared is "connect" or "subscription" or "event_verification")
                stage = declared;
            if (error is HttpRequestException request && request.StatusCode.HasValue && (int)request.StatusCode.Value is >= 100 and <= 599)
                status ??= (int)request.StatusCode.Value;
            if (error is WebSocketException socket)
                socketError = Enum.IsDefined(typeof(WebSocketError), socket.WebSocketErrorCode) ? socket.WebSocketErrorCode.ToString() : "unknown";
        }
        return new(categories.Count == 0 ? "none" : string.Join("/", categories), stage, status, socketError);
    }

    internal static void Record(ILogger logger, ProviderType provider, bool activation, Exception error,
        long? elapsedMilliseconds = null)
    {
        var summary = Capture(error);
        logger.LogWarning(new EventId(4831, "CqlNativeFailure"),
            "CQL native failure provider={Provider} phase={Phase} stage={Stage} category={Category} httpStatus={HttpStatus} websocket={WebSocketError} elapsedMilliseconds={ElapsedMilliseconds}",
            provider, activation ? "activation" : "query", summary.Stage, summary.Categories, summary.HttpStatus, summary.WebSocketError, elapsedMilliseconds);
    }

    internal static void Query(HttpContext context, ProviderType provider, Exception error)
    {
        var logger = context.RequestServices?.GetService<ILoggerFactory>()?.CreateLogger("OASIS.CQL.NativeFailure");
        if (logger != null) Record(logger, provider, false, error);
    }
}
