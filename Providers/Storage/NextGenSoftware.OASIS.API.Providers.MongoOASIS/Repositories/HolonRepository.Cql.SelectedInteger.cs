using System;
using System.IO;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal static CosmicProjectedNumericPacket ReadNativeCqlSelectedNumeric(BsonDocument envelope, CosmicProjectedNumericIntent intent)
    {
        if (envelope == null || envelope.ElementCount != 1 || !envelope.TryGetValue("packet", out var raw) || !raw.IsBsonDocument)
            throw new InvalidDataException("Selected integer reduction requires one exact native packet envelope.");
        var input = Nullable.GetUnderlyingType(intent.InputType) ?? intent.InputType;
        if (input != typeof(int) && input != typeof(long))
            throw new InvalidDataException("The selected integer packet requires its signed integer domain.");
        var required = Nullable.GetUnderlyingType(intent.InputType) == null;
        var value = intent.UsesInt64DoubleAverage
            ? ReadNativeCqlCheckedNumeric(raw.AsBsonDocument, false, required)
            : ReadNativeCqlNumeric(raw.AsBsonDocument, false, required: required);
        return new CosmicProjectedNumericPacket(value.Count, new[] { value.High, value.Low }, value.PrefixOverflow);
    }
}
