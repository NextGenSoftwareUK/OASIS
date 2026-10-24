using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    private async Task<List<T>> ReadRecordCollectionAsync<T>(string table,int version,bool exportAllVersions=false) where T:class,IHolonBase
    {
        if(version<0&&!(exportAllVersions&&version==-1))throw new ArgumentOutOfRangeException(nameof(version));
        var expected=typeof(T)==typeof(Core.Holons.Avatar)?"OASIS_AVATARS":typeof(T)==typeof(Core.Holons.AvatarDetail)?"OASIS_AVATAR_DETAILS":typeof(T)==typeof(Core.Holons.Holon)?"OASIS_HOLONS":null;
        if(table!=expected)throw new ArgumentException("Canonical Oracle collection must match its record type.");
        var rows=new List<T>();
        await using var connection=new OracleConnection(_connectionString);await connection.OpenAsync();
        var validateHolon=typeof(T)==typeof(Core.Holons.Holon);
        var validateAvatar=typeof(T)==typeof(Core.Holons.Avatar);
        var validateIdentity=validateAvatar||typeof(T)==typeof(Core.Holons.AvatarDetail);
        await using var command=CreateCommand($"SELECT ID,DATA_JSON{(validateHolon?",HOLON_TYPE,IS_DELETED,PARENT_HOLON_ID":validateAvatar?",IS_DELETED,USERNAME,EMAIL":validateIdentity?",USERNAME,EMAIL":"")} FROM {table}",connection);
        await using var reader=await command.ExecuteReaderAsync();
        while(await reader.ReadAsync()){
            var id=new Guid((byte[])reader.GetValue(0));
            if(version==-1){
                var (_,document)=ReadRecordDocument<T>(id,reader.GetString(1),validateAvatar?reader.GetInt32(2):null,validateIdentity?ReadNativeIdentityProjection(reader,validateAvatar?3:2):null,validateHolon?ReadNativeHolonProjection(reader,2):null);
                rows.AddRange(RecordHistory.ReadAll(id,document,value=>Deserialize<T>(value)).Where(value=>!value.IsDeleted));
                continue;
            }
            var row=await ReadRecordVersionAsync<T>(id,reader.GetString(1),version,validateAvatar?reader.GetInt32(2):null,validateIdentity?ReadNativeIdentityProjection(reader,validateAvatar?3:2):null,validateHolon?ReadNativeHolonProjection(reader,2):null);
            if(row!=null&&!row.IsDeleted)rows.Add(row);
        }
        return rows;
    }
}
