using System;
using System.Threading.Tasks;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS;

public partial class InfluxDBOASIS
{
    /// <summary>Await the native HTTP write acknowledgement before caller publication.</summary>
    protected virtual Task PersistHolonAsync(Guid id, Guid parentId, string type, bool deleted, string json)
        => WritePointAsync(PointData.Measurement("oasis_holons")
            .Tag("id", id.ToString("D"))
            .Tag("parent_holon_id", parentId.ToString("D"))
            .Tag("holon_type", type)
            .Field("is_deleted", deleted)
            .Field("data_json", json)
            .Timestamp(DateTime.UtcNow, WritePrecision.Ns));
}
