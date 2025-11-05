namespace ScGen.Lib.Shared.Extensions;

public static class JsonExtensions
{
    public static object? EthereumConvertJToken(this JToken? token)
    {
        if (token == null) return null;

        switch (token.Type)
        {
            case JTokenType.Object:
                IDictionary<string, object?> dict = new Dictionary<string, object?>();
                foreach (JProperty prop in ((JObject)token).Properties())
                {
                    dict[prop.Name] = EthereumConvertJToken(prop.Value);
                }

                return dict;

            case JTokenType.Array:
                List<object?> list = [];
                foreach (JToken item in (JArray)token)
                {
                    list.Add(EthereumConvertJToken(item));
                }

                return list;

            case JTokenType.Integer:
                return ((JValue)token).ToObject<long>();

            case JTokenType.Float:
                return ((JValue)token).ToObject<double>();

            case JTokenType.Boolean:
                return ((JValue)token).ToObject<bool>();

            case JTokenType.Null:
                return null;

            default:
                return ((JValue)token).ToString(CultureInfo.InvariantCulture);
        }
    }

    public static object? EthereumCleanModel(this object? node)
    {
        if (node is IDictionary<string, object?> dict)
        {
            List<string> keys = new List<string>(dict.Keys);
            foreach (string k in keys)
            {
                object? v = dict[k];
                object? cleaned = EthereumCleanModel(v);
                if (cleaned == null)
                {
                    dict.Remove(k);
                }
                else
                {
                    dict[k] = cleaned;
                }
            }

            return dict.Count == 0 ? null : dict;
        }

        if (node is IList<object> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                object? cleaned = EthereumCleanModel(list[i]);
                if (cleaned == null)
                    list.RemoveAt(i);
                else
                    list[i] = cleaned;
            }

            return list.Count == 0 ? null : list;
        }

        return node;
    }


    public static object? SolanaConvertJToken(this JToken? token)
    {
        if (token == null) return null;
        if (token.Type == JTokenType.Object)
        {
            IDictionary<string, object?> dict = new Dictionary<string, object?>();
            foreach (JProperty prop in ((JObject)token).Properties())
            {
                dict[prop.Name] = SolanaConvertJToken(prop.Value);
            }

            return dict;
        }

        if (token.Type == JTokenType.Array)
        {
            List<object?> list = [];
            foreach (JToken item in (JArray)token)
            {
                list.Add(SolanaConvertJToken(item));
            }

            return list;
        }

        if (token.Type == JTokenType.Integer)
            return ((JValue)token).ToObject<long>();
        if (token.Type == JTokenType.Float)
            return ((JValue)token).ToObject<double>();
        if (token.Type == JTokenType.Boolean)
            return ((JValue)token).ToObject<bool>();
        if (token.Type == JTokenType.Null)
            return null;
        return ((JValue)token).ToString(CultureInfo.InvariantCulture);
    }

    public static object? SolanaCleanModel(this object? node)
    {
        if (node is IDictionary<string, object?> dict)
        {
            List<string> keys = new List<string>(dict.Keys);
            foreach (string k in keys)
            {
                object? v = dict[k];
                object? cleaned = SolanaCleanModel(v);
                if (cleaned == null)
                {
                    dict.Remove(k);
                }
                else
                {
                    dict[k] = cleaned;
                }
            }

            if (dict.Count == 0) return null;
            return dict;
        }

        if (node is IList<object> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                object? cleaned = SolanaCleanModel(list[i]);
                if (cleaned == null)
                    list.RemoveAt(i);
                else
                    list[i] = cleaned;
            }

            if (list.Count == 0) return null;
            return list;
        }

        return node;
    }


    public static object? RadixConvertJToken(this JToken? token)
    {
        if (token == null) return null;

        return token.Type switch
        {
            JTokenType.Object => RadixConvertJObject((JObject)token),
            JTokenType.Array => RadixConvertJArray((JArray)token),
            JTokenType.Integer => ((JValue)token).ToObject<long>(),
            JTokenType.Float => ((JValue)token).ToObject<double>(),
            JTokenType.Boolean => ((JValue)token).ToObject<bool>(),
            JTokenType.Null => null,
            _ => ((JValue)token).ToString(CultureInfo.InvariantCulture)
        };
    }

    private static IDictionary<string, object?> RadixConvertJObject(JObject jObject)
    {
        Dictionary<string, object?> dict = new Dictionary<string, object?>();
        foreach (JProperty prop in jObject.Properties())
        {
            dict[prop.Name] = RadixConvertJToken(prop.Value);
        }

        return dict;
    }

    private static List<object?> RadixConvertJArray(JArray jArray)
    {
        List<object?> list = new List<object?>(jArray.Count);
        foreach (JToken item in jArray)
        {
            list.Add(RadixConvertJToken(item));
        }

        return list;
    }

    public static object? RadixCleanModel(this object? node)
    {
        if (node is IDictionary<string, object?> dict)
        {
            List<string> keys = new List<string>(dict.Keys);
            foreach (string k in keys)
            {
                object? v = dict[k];
                object? cleaned = RadixCleanModel(v);
                if (cleaned == null)
                {
                    dict.Remove(k);
                }
                else
                {
                    dict[k] = cleaned;
                }
            }

            return dict.Count == 0 ? null : dict;
        }

        if (node is IList<object?> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                object? cleaned = RadixCleanModel(list[i]);
                if (cleaned == null)
                    list.RemoveAt(i);
                else
                    list[i] = cleaned;
            }

            return list.Count == 0 ? null : list;
        }

        return node;
    }
}