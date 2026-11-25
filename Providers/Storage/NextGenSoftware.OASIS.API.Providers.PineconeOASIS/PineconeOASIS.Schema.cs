using System.IO;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.PineconeOASIS;

internal static class PineconeIndexValidation
{
    // Storage needs exactly one property of the index: it accepts one-dimensional dense vectors. A dimension of 1
    // proves that (sparse indexes have no dimension). vectorType and metric are not part of every API version's
    // statistics response; when reported they must agree, but storage never depends on the metric.
    // A data-plane host is explicit; activation never creates or silently replaces an index.
    public static void Validate(JsonElement index)
    {
        if (!index.TryGetProperty("dimension", out var dimension) || dimension.GetInt32() != 1)
            throw new InvalidDataException("Pinecone requires a one-dimensional dense document-storage index.");
        if (index.TryGetProperty("vectorType", out var vectorType) && vectorType.GetString() != "dense")
            throw new InvalidDataException("Pinecone requires a one-dimensional dense document-storage index.");
        if (index.TryGetProperty("metric", out var metricValue) &&
            metricValue.GetString() is not ("cosine" or "dotproduct" or "euclidean"))
            throw new InvalidDataException("Unsupported Pinecone index metric.");
        if (index.GetProperty("totalVectorCount").GetInt64() < 0 ||
            !index.GetProperty("indexFullness").TryGetDouble(out var fullness) || !double.IsFinite(fullness) || fullness < 0 || fullness > 1)
            throw new InvalidDataException("Invalid Pinecone native index statistics.");
        var namespaces = index.GetProperty("namespaces");
        if (namespaces.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Pinecone namespace map is missing.");
        foreach (var ns in namespaces.EnumerateObject())
            if (ns.Value.GetProperty("vectorCount").GetInt64() < 0)
                throw new InvalidDataException("Pinecone namespace count is invalid.");
    }
}
