using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization
{
    // The same generated wire contract runs in ONODE, NativeAOT and Unity IL2CPP.
    // PayloadJson remains opaque: the transport never discovers application types by reflection.
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(IssueHyperDriveOfflineSessionGrantRequest))]
    [JsonSerializable(typeof(BindHyperDrivePeerRequest))]
    [JsonSerializable(typeof(SyncExchangeRequest))]
    [JsonSerializable(typeof(OASISResult<SyncExchangeResponse>))]
    [JsonSerializable(typeof(OASISResult<HyperDriveOfflineSessionGrant>))]
    [JsonSerializable(typeof(OASISResult<bool>))]
    [JsonSerializable(typeof(HyperDriveCommandOutcome))]
    [JsonSerializable(typeof(HyperDriveQuestProgressCommand))]
    [JsonSerializable(typeof(HyperDriveQuestProgressProjection))]
    [JsonSerializable(typeof(HyperDriveInventoryGrantCommand))]
    [JsonSerializable(typeof(HyperDriveGeoNftCollectionCommand))]
    [JsonSerializable(typeof(HyperDriveAvatarProjection))]
    [JsonSerializable(typeof(HyperDriveAvatarDetailProjection))]
    [JsonSerializable(typeof(HyperDriveAvatarGameplayCommand))]
    [JsonSerializable(typeof(HyperDriveQuestLifecycleCommand))]
    public partial class HyperDriveJsonContext : JsonSerializerContext { }

    public static class HyperDriveJson
    {
        public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, TypeInfo<T>());
        public static T Deserialize<T>(string json) => JsonSerializer.Deserialize(json, TypeInfo<T>());

        private static JsonTypeInfo<T> TypeInfo<T>() =>
            HyperDriveJsonContext.Default.GetTypeInfo(typeof(T)) as JsonTypeInfo<T> ??
            throw new NotSupportedException($"'{typeof(T)}' is not a HyperDrive JSON contract.");
    }
}
