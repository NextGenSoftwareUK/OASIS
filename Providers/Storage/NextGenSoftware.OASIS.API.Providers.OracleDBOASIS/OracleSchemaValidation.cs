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
        foreach(var table in expected.Keys){
            var key=table=="OASIS_WRITE_RECEIPTS"?"OPERATION_ID":"ID";
            await using var command=CreateCommand(@"SELECT cc.COLUMN_NAME,c.STATUS,c.VALIDATED,c.DEFERRABLE,i.STATUS
FROM USER_CONSTRAINTS c JOIN USER_CONS_COLUMNS cc ON cc.CONSTRAINT_NAME=c.CONSTRAINT_NAME AND cc.TABLE_NAME=c.TABLE_NAME
LEFT JOIN USER_INDEXES i ON i.INDEX_NAME=c.INDEX_NAME
WHERE c.TABLE_NAME=:TableName AND c.CONSTRAINT_TYPE='P' ORDER BY cc.POSITION",connection);
            command.Parameters.Add("TableName",OracleDbType.Varchar2).Value=table;
            var columns=new List<string>();string status=null,validated=null,deferrable=null,indexStatus=null;
            await using var reader=await command.ExecuteReaderAsync();
            while(await reader.ReadAsync()){
                columns.Add(reader.GetString(0));status=reader.GetString(1);validated=reader.GetString(2);deferrable=reader.GetString(3);
                indexStatus=reader.IsDBNull(4)?null:reader.GetString(4);
            }
            ValidateStoragePrimaryKey(table,key,columns,status,validated,deferrable,indexStatus);
        }
    }

    private static void ValidateStoragePrimaryKey(string table,string key,IReadOnlyList<string> columns,
        string status,string validated,string deferrable,string indexStatus)
    {
        if(columns.Count!=1||columns[0]!=key||status!="ENABLED"||validated!="VALIDATED"
            ||deferrable!="NOT DEFERRABLE"||indexStatus!="VALID")
            throw new InvalidDataException($"Oracle schema {table} requires an enabled, validated, immediate single-column {key} primary key with a valid index.");
    }

    private static void ValidateStorageColumn(string table,string column,string required,string actual,int length)
    {
        var compatible=required=="TIMESTAMP"?actual=="TIMESTAMP"||actual.StartsWith("TIMESTAMP(",StringComparison.Ordinal)&&actual.EndsWith(")",StringComparison.Ordinal):actual==required;
        if(!compatible||required=="RAW"&&length!=16||required=="VARCHAR2"&&length<(column=="REQUEST_HASH"?64:256))
            throw new InvalidDataException($"Oracle schema {table}.{column} has incompatible type or capacity; expected {required}.");
    }
}
