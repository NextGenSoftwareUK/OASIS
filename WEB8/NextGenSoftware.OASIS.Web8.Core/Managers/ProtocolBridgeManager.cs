using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Net;
using NextGenSoftware.OASIS.Web8.Core.Enums;
using NextGenSoftware.OASIS.Web8.Core.Models;

namespace NextGenSoftware.OASIS.Web8.Core.Managers
{
    /// <summary>
    /// Translates any external system's wire format into the unified MeshMessage envelope and back - "any
    /// civilisation's communication standard is translated through the holonic abstraction layer" implemented as
    /// real, deterministic format adapters rather than narrative. New formats are added by extending BridgeFormat
    /// and the corresponding case here, the same extensibility pattern as AIProviderManager's provider switch.
    /// </summary>
    public class ProtocolBridgeManager
    {
        public MeshMessage TranslateInbound(string rawPayload, BridgeFormat format, Guid sourceNodeId, Guid destinationNodeId)
        {
            string normalisedJson = format switch
            {
                BridgeFormat.Json => rawPayload,
                BridgeFormat.FormUrlEncoded => FormUrlEncodedToJson(rawPayload),
                BridgeFormat.PlainText => JsonSerializer.Serialize(new { text = rawPayload }),
                _ => throw new NotSupportedException($"Bridge format '{format}' is not supported."),
            };

            return new MeshMessage { SourceNodeId = sourceNodeId, DestinationNodeId = destinationNodeId, Payload = normalisedJson };
        }

        public string TranslateOutbound(MeshMessage message, BridgeFormat targetFormat)
        {
            return targetFormat switch
            {
                BridgeFormat.Json => message.Payload,
                BridgeFormat.FormUrlEncoded => JsonToFormUrlEncoded(message.Payload),
                BridgeFormat.PlainText => ExtractPlainText(message.Payload),
                _ => throw new NotSupportedException($"Bridge format '{targetFormat}' is not supported."),
            };
        }

        private static string FormUrlEncodedToJson(string raw)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>();

            foreach (string pair in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] kv = pair.Split('=', 2);
                string key = WebUtility.UrlDecode(kv[0]);
                string value = kv.Length > 1 ? WebUtility.UrlDecode(kv[1]) : string.Empty;
                pairs[key] = value;
            }

            return JsonSerializer.Serialize(pairs);
        }

        private static string JsonToFormUrlEncoded(string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            IEnumerable<string> pairs = doc.RootElement.EnumerateObject()
                .Select(p => $"{WebUtility.UrlEncode(p.Name)}={WebUtility.UrlEncode(p.Value.ToString())}");

            return string.Join("&", pairs);
        }

        private static string ExtractPlainText(string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("text", out JsonElement textElement) ? textElement.GetString() : json;
        }
    }
}
