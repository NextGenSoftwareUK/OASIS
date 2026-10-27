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
        if (input != typeof(int) && input != typeof(long) && input != typeof(ulong) && input != typeof(decimal) && input != typeof(float) && input != typeof(double))
            throw new InvalidDataException("The selected integer packet requires its declared exact numeric domain.");
        var decimalInput = input == typeof(decimal);
        var floatingWidth = input == typeof(float) || input == typeof(double) ? MongoCqlTranslator.SelectedNumericWidth(input) : 0;
        var required = Nullable.GetUnderlyingType(intent.InputType) == null;
        var value = intent.UsesInt64DoubleAverage
            ? ReadNativeCqlCheckedNumeric(raw.AsBsonDocument, false, required)
            : ReadNativeCqlNumeric(raw.AsBsonDocument, false, decimalInput, required, floatingWidth);
        return new CosmicProjectedNumericPacket(value.Count, value.DecimalParts ?? value.FloatingParts ?? new[] { value.High, value.Low }, value.PrefixOverflow);
    }
}
