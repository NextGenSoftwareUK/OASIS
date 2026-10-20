using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    private async Task<List<T>> ReadRecordCollectionAsync<T>(string table,int version) where T:class,IHolonBase
    {
        if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
        var expected=typeof(T)==typeof(Core.Holons.Avatar)?"OASIS_AVATARS":typeof(T)==typeof(Core.Holons.AvatarDetail)?"OASIS_AVATAR_DETAILS":typeof(T)==typeof(Core.Holons.Holon)?"OASIS_HOLONS":null;
        if(table!=expected)throw new ArgumentException("Canonical Oracle collection must match its record type.");
        var rows=new List<T>();
        await using var connection=new OracleConnection(_connectionString);await connection.OpenAsync();
        var validateHolon=typeof(T)==typeof(Core.Holons.Holon)&&version==0;
        await using var command=CreateCommand($"SELECT ID,DATA_JSON{(validateHolon?",HOLON_TYPE,IS_DELETED,PARENT_HOLON_ID":"")} FROM {table}",connection);
        await using var reader=await command.ExecuteReaderAsync();
        while(await reader.ReadAsync()){
            var id=new Guid((byte[])reader.GetValue(0));var row=await ReadRecordVersionAsync<T>(id,reader.GetString(1),version);
            if(validateHolon&&row is IHolon holon&&((int)holon.HolonType!=reader.GetInt32(2)||(holon.IsDeleted?1:0)!=reader.GetInt32(3)
                ||holon.ParentHolonId!=(reader.IsDBNull(4)?Guid.Empty:new Guid((byte[])reader.GetValue(4)))))
                throw new System.IO.InvalidDataException("Oracle native holon type/deletion/parent projection differs from its canonical record.");
            if(row!=null&&!row.IsDeleted)rows.Add(row);
        }
        return rows;
    }
}
