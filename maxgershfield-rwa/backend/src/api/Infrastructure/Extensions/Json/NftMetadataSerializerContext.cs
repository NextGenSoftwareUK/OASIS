using System.Text.Json.Serialization;

namespace Infrastructure.Extensions.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Nft))]
public partial class NftMetadataSerializerContext : JsonSerializerContext
{
}