using System;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.State;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;

/// <summary>
/// Helper for parsing Radix component state and extracting KeyValueStore values
/// </summary>
public static class RadixStateHelper
{
    /// <summary>
    /// Extracts a value from component state's KeyValueStore by key
    /// Supports both u64 keys (for avatars/holons) and string keys (for indexes)
    /// </summary>
    public static string? ExtractKeyValueStoreEntry(
        StateEntityDetailsResponse? response,
        string componentAddress,
        string storeName,
        object key)
    {
        if (response?.Items == null || response.Items.Count == 0)
            return null;

        // Find the component entity
        var componentItem = response.Items.FirstOrDefault(item => 
            item.Address.Equals(componentAddress, StringComparison.OrdinalIgnoreCase) &&
            item.Type?.Contains("Component", StringComparison.OrdinalIgnoreCase) == true);

        if (componentItem?.State?.Fields?.KeyValueStoreEntries == null)
            return null;

        // Convert key to string format for lookup
        string keyString = key switch
        {
            ulong ulongKey => ulongKey.ToString(),
            long longKey => longKey.ToString(),
            int intKey => intKey.ToString(),
            uint uintKey => uintKey.ToString(),
            string strKey => strKey,
            _ => key.ToString() ?? string.Empty
        };

        // Try to find the entry by key
        // Note: The actual structure might be different - this is a flexible approach
        // We'll try multiple ways to find the key-value pair
        
        // Method 1: Direct key lookup if keys are stored as strings
        if (componentItem.State.Fields.KeyValueStoreEntries.TryGetValue(keyString, out var entry))
        {
            return ExtractValueFromEntry(entry);
        }

        // Method 2: Search through entries if structure is different
        foreach (var kvp in componentItem.State.Fields.KeyValueStoreEntries)
        {
            var entryKey = kvp.Value.Key;
            if (entryKey != null && entryKey.ToString() == keyString)
            {
                return ExtractValueFromEntry(kvp.Value);
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the string value from a KeyValueStore entry
    /// Handles different value representations (direct string, JSON, bytes, etc.)
    /// </summary>
    private static string? ExtractValueFromEntry(KeyValueStoreEntry entry)
    {
        if (entry.Value == null)
            return null;

        // Try direct string value first
        if (!string.IsNullOrEmpty(entry.Value.StringValue))
        {
            return entry.Value.StringValue;
        }

        // Try value field as string
        if (entry.Value.Value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                return jsonElement.GetString();
            }
            // If it's an object, serialize it back to JSON
            if (jsonElement.ValueKind == JsonValueKind.Object || jsonElement.ValueKind == JsonValueKind.Array)
            {
                return jsonElement.GetRawText();
            }
        }

        if (entry.Value.Value is string strValue)
        {
            return strValue;
        }

        // Try to serialize value to JSON string
        try
        {
            return JsonSerializer.Serialize(entry.Value.Value);
        }
        catch
        {
            // If serialization fails, return null
            return null;
        }
    }

    /// <summary>
    /// Extracts value from component state using flexible JSON parsing
    /// This method handles different response structures that Gateway API might return
    /// </summary>
    public static string? ExtractValueFromComponentState(
        string jsonResponse,
        string storeName,
        object key)
    {
        try
        {
            var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            // Try multiple path structures that Gateway API might use
            var paths = new[]
            {
                $"items[0].state.fields.{storeName}",
                $"items[0].state.{storeName}",
                $"state.fields.{storeName}",
                $"state.{storeName}",
                storeName
            };

            string keyString = key switch
            {
                ulong ulongKey => ulongKey.ToString(),
                long longKey => longKey.ToString(),
                int intKey => intKey.ToString(),
                uint uintKey => uintKey.ToString(),
                string strKey => strKey,
                _ => key.ToString() ?? string.Empty
            };

            foreach (var path in paths)
            {
                var element = NavigateJsonPath(root, path);
                if (element.HasValue)
                {
                    // Try to find the key in the store
                    if (element.Value.ValueKind == JsonValueKind.Object)
                    {
                        // Try direct key lookup
                        if (element.Value.TryGetProperty(keyString, out var valueProp))
                        {
                            return ExtractJsonValue(valueProp);
                        }

                        // Try iterating through properties
                        foreach (var prop in element.Value.EnumerateObject())
                        {
                            if (prop.Name == keyString || prop.Value.GetRawText().Contains(keyString))
                            {
                                return ExtractJsonValue(prop.Value);
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Parsing failed, return null
        }

        return null;
    }

    /// <summary>
    /// Navigates a JSON path (simplified - handles array[0] and dot notation)
    /// </summary>
    private static JsonElement? NavigateJsonPath(JsonElement root, string path)
    {
        var parts = path.Split('.');
        JsonElement current = root;

        foreach (var part in parts)
        {
            if (part.Contains('['))
            {
                var arrayPart = part.Substring(0, part.IndexOf('['));
                var indexPart = part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1);
                
                if (!string.IsNullOrEmpty(arrayPart) && current.TryGetProperty(arrayPart, out var arrayElement))
                {
                    current = arrayElement;
                }
                
                if (int.TryParse(indexPart, out var index) && current.ValueKind == JsonValueKind.Array)
                {
                    current = current[index];
                }
            }
            else
            {
                if (!current.TryGetProperty(part, out var next))
                    return null;
                current = next;
            }
        }

        return current;
    }

    /// <summary>
    /// Extracts a string value from a JsonElement
    /// </summary>
    private static string? ExtractJsonValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Object:
            case JsonValueKind.Array:
                return element.GetRawText();
            case JsonValueKind.Number:
                return element.GetRawText();
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean().ToString();
            default:
                return element.GetRawText();
        }
    }
}

