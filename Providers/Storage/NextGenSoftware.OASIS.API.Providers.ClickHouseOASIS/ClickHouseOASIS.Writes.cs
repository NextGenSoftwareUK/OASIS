using System;
using System.Threading.Tasks;
using ClickHouse.Client.ADO;

namespace NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS;

public partial class ClickHouseOASIS
{
    /// <summary>One synchronous native insert acknowledgement precedes caller publication.</summary>
    protected virtual async Task PersistHolonAsync(Guid id, Guid parentId, string type, bool deleted, string json)
    {
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandTimeout = 20;
        command.CommandText = "INSERT INTO oasis.holons (id,parent_holon_id,holon_type,is_deleted,data_json) " +
            "SETTINGS async_insert=0 VALUES ({id:UUID},{parent:UUID},{type:String},{deleted:UInt8},{json:String})";
        foreach (var pair in new (string Name, object Value)[] {
            ("id", id), ("parent", parentId), ("type", type), ("deleted", (byte)(deleted ? 1 : 0)), ("json", json) })
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = pair.Name;
            parameter.Value = pair.Value;
            command.Parameters.Add(parameter);
        }
        await command.ExecuteNonQueryAsync();
    }
}
