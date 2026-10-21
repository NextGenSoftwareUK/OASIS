using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Provider entry point to the single Core canonical value-equivalence owner; excludes no fields.</summary>
public static class StoredRecordPayloadEquality
{
    public static bool Equivalent(JToken expected,JToken actual)
        =>StoredRecordValueEquality.Equivalent(expected,actual);

    internal static bool IsNumber(JToken token)=>StoredRecordValueEquality.IsNumber(token);

    internal static string CanonicalNumberText(JToken token)=>StoredRecordValueEquality.CanonicalNumberText(token);
}
