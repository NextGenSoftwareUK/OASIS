using System;
using System.Globalization;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Theory]
    [InlineData("0.1")] [InlineData("1.401298E-45")] [InlineData("3.4028235E+38")]
    public Task NativeSingleStorageRoundTripsWithoutTextOrAcknowledgementDrift(string text)
    {
        var value = float.Parse(text, CultureInfo.InvariantCulture);
        return WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var document = await fixture.Records.Find(new BsonDocument("HolonId", id.ToString("D"))).SingleAsync();
            var number = document["MetaData"]["sample"];
            Assert.True(number.IsDouble || number.IsDecimal128 || number.IsInt32 || number.IsInt64,
                "A native floating value must not become JSON text.");
            Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits((float)number.ToDouble()));

            var loaded = await fixture.Provider.LoadHolonAsync(id, loadChildren: false);
            Assert.False(loaded.IsError, loaded.Message + loaded.Exception);
            Assert.True(StoredRecordValueEquality.Equivalent(StoredRecordJson.CaptureInputToken(value),
                StoredRecordJson.CaptureInputToken(loaded.Result.MetaData["sample"])));
        }, width: 0, configure: root => root.MetaData["sample"] = value);
    }
}
