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
        var validateAvatar=typeof(T)==typeof(Core.Holons.Avatar);
        var validateIdentity=validateAvatar||typeof(T)==typeof(Core.Holons.AvatarDetail);
        await using var command=CreateCommand($"SELECT ID,DATA_JSON{(validateHolon?",HOLON_TYPE,IS_DELETED,PARENT_HOLON_ID":validateAvatar?",IS_DELETED,USERNAME,EMAIL":validateIdentity?",USERNAME,EMAIL":"")} FROM {table}",connection);
        await using var reader=await command.ExecuteReaderAsync();
        while(await reader.ReadAsync()){
            var id=new Guid((byte[])reader.GetValue(0));var row=await ReadRecordVersionAsync<T>(id,reader.GetString(1),version,validateAvatar?reader.GetInt32(2):null,validateIdentity?ReadNativeIdentityProjection(reader,validateAvatar?3:2):null);
            if(validateHolon&&row is IHolon holon)
                ValidateNativeHolonProjection(holon,reader.GetInt32(2),reader.GetInt32(3),reader.IsDBNull(4)?Guid.Empty:new Guid((byte[])reader.GetValue(4)));
            if(row!=null&&!row.IsDeleted)rows.Add(row);
        }
        return rows;
    }
}
