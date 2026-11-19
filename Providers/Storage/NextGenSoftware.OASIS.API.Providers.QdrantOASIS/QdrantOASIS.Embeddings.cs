using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.QdrantOASIS;

public sealed record QdrantHolonMatch(IHolon Holon, double Score);

public partial class QdrantOASIS
{
    /// <summary>Replace the native embedding of a stored holon without replacing its payload.</summary>
    public async Task<OASISResult<bool>> SetHolonEmbeddingAsync(Guid id, IReadOnlyList<float> embedding)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (id == Guid.Empty)
                throw new ArgumentException("A stored holon ID is required.", nameof(id));
            var vector = ValidateEmbedding(embedding);
            var point = await GetPointAsync("oasis_holons", id.ToString("D"));
            if (point == null || GetPayloadBool(point.Value, "is_deleted"))
            {
                OASISErrorHandling.HandleError(ref result, "No live Qdrant holon exists for that ID.");
                result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                return result;
            }
            var response = await QdrantPutAsync("/collections/oasis_holons/points/vectors?wait=true",
                new { points = new[] { new { id = id.ToString("D"), vector } } });
            var receipt = response.GetProperty("result");
            if (receipt.GetProperty("status").GetString() != "completed")
                throw new InvalidDataException("Native embedding update did not complete.");
            result.MetaData["Qdrant.OperationId"] = receipt.GetProperty("operation_id").GetUInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);
            result.Result = true;
            result.IsSaved = true;
            result.SavedCount = 1;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Qdrant embedding update failed: {ex.Message}", ex);
        }
        return result;
    }

    /// <summary>Return live holons ranked by native Qdrant cosine similarity.</summary>
    public async Task<OASISResult<IReadOnlyList<QdrantHolonMatch>>> SearchHolonsByEmbeddingAsync(IReadOnlyList<float> embedding, int limit = 10)
    {
        var result = new OASISResult<IReadOnlyList<QdrantHolonMatch>>();
        try
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
            var vector = ValidateEmbedding(embedding);
            var response = await QdrantPostAsync("/collections/oasis_holons/points/query", new
            {
                query = vector,
                limit,
                with_payload = true,
                with_vector = false,
                filter = new { must_not = new[] { new { key = "is_deleted", match = new { value = true } } } }
            });
            if (!HasOkStatus(response))
                throw new InvalidDataException("Native embedding query was not acknowledged.");
            var matches = new List<QdrantHolonMatch>();
            var ids = new HashSet<Guid>();
            foreach (var point in response.GetProperty("result").GetProperty("points").EnumerateArray())
            {
                var id = point.GetProperty("id").GetGuid();
                var score = point.GetProperty("score").GetDouble();
                var holon = Des<Holon>(GetPayloadStr(point, "data_json"));
                if (holon == null || holon.Id != id || holon.IsDeleted || !ids.Add(id) || !double.IsFinite(score))
                    throw new InvalidDataException("Native embedding query returned an invalid holon or score.");
                matches.Add(new QdrantHolonMatch(holon, score));
            }
            if (matches.Count > limit)
                throw new InvalidDataException("Native embedding query returned more points than requested.");
            result.Result = matches;
            result.IsLoaded = true;
            result.LoadedCount = matches.Count;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Qdrant embedding query failed: {ex.Message}", ex);
        }
        return result;
    }

    private float[] ValidateEmbedding(IReadOnlyList<float> embedding)
    {
        ArgumentNullException.ThrowIfNull(embedding);
        var vector = embedding.ToArray();
        if (vector.Length != _vectorSize || vector.Any(value => !float.IsFinite(value)) || vector.All(value => value == 0))
            throw new ArgumentException($"A finite nonzero embedding with {_vectorSize} coordinates is required.", nameof(embedding));
        return vector;
    }
}
