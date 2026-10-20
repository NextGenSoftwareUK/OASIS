using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    // ORA-00955 means a name exists, not that its schema matches this provider.
    // Read only: incompatible existing objects must be reviewed, never repaired here.
    private static async Task ValidateStorageSchemaAsync(OracleConnection connection)
    {
        var expected=new Dictionary<string,Dictionary<string,string>>{
            ["OASIS_AVATARS"]=new(){{"ID","RAW"},{"USERNAME","VARCHAR2"},{"EMAIL","VARCHAR2"},{"IS_DELETED","NUMBER"},{"CREATED_DATE","TIMESTAMP"},{"MODIFIED_DATE","TIMESTAMP"},{"DATA_JSON","CLOB"}},
            ["OASIS_AVATAR_DETAILS"]=new(){{"ID","RAW"},{"USERNAME","VARCHAR2"},{"EMAIL","VARCHAR2"},{"DATA_JSON","CLOB"}},
            ["OASIS_HOLONS"]=new(){{"ID","RAW"},{"PARENT_HOLON_ID","RAW"},{"HOLON_TYPE","NUMBER"},{"IS_DELETED","NUMBER"},{"CREATED_DATE","TIMESTAMP"},{"MODIFIED_DATE","TIMESTAMP"},{"DATA_JSON","CLOB"}},
            ["OASIS_WRITE_RECEIPTS"]=new(){{"OPERATION_ID","RAW"},{"REQUEST_HASH","VARCHAR2"},{"RESULT_JSON","CLOB"},{"COMMITTED_AT","TIMESTAMP"}}
        };
        foreach(var table in expected){
            await using var command=CreateCommand("SELECT COLUMN_NAME,DATA_TYPE,DATA_LENGTH FROM USER_TAB_COLUMNS WHERE TABLE_NAME=:TableName",connection);
            command.Parameters.Add("TableName",OracleDbType.Varchar2).Value=table.Key;
            var missing=new HashSet<string>(table.Value.Keys,StringComparer.Ordinal);
            await using var reader=await command.ExecuteReaderAsync();
            while(await reader.ReadAsync()){
                var column=reader.GetString(0);
                if(!table.Value.TryGetValue(column,out var required))continue;
                ValidateStorageColumn(table.Key,column,required,reader.GetString(1),reader.GetInt32(2));
                missing.Remove(column);
            }
            if(missing.Count!=0)throw new InvalidDataException($"Oracle schema {table.Key} is missing required columns: {string.Join(",",missing)}.");
        }
    }

    private static void ValidateStorageColumn(string table,string column,string required,string actual,int length)
    {
        var compatible=required=="TIMESTAMP"?actual=="TIMESTAMP"||actual.StartsWith("TIMESTAMP(",StringComparison.Ordinal)&&actual.EndsWith(")",StringComparison.Ordinal):actual==required;
        if(!compatible||required=="RAW"&&length!=16||required=="VARCHAR2"&&length<(column=="REQUEST_HASH"?64:256))
            throw new InvalidDataException($"Oracle schema {table}.{column} has incompatible type or capacity; expected {required}.");
    }
}
