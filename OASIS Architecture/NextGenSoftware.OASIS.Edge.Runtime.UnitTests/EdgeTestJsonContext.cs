using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

[JsonSerializable(typeof(OASISEdgeRuntimeTests.TestEntity), TypeInfoPropertyName = "RuntimeTestEntity")]
[JsonSerializable(typeof(OASISEdgeNativeEndpointTests.TestEntity), TypeInfoPropertyName = "EndpointTestEntity")]
internal partial class EdgeTestJsonContext : JsonSerializerContext { }
