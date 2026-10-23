using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    // ORA-00955 means a name exists, not that its schema matches this provider.
    // Read only: incompatible existing objects must be reviewed, never repaired here.
    private static readonly Dictionary<string,Dictionary<string,string>> StorageSchema=new(){
            ["OASIS_AVATARS"]=new(){{"ID","RAW"},{"USERNAME","VARCHAR2"},{"EMAIL","VARCHAR2"},{"IS_DELETED","NUMBER"},{"CREATED_DATE","TIMESTAMP"},{"MODIFIED_DATE","TIMESTAMP"},{"DATA_JSON","CLOB"}},
            ["OASIS_AVATAR_DETAILS"]=new(){{"ID","RAW"},{"USERNAME","VARCHAR2"},{"EMAIL","VARCHAR2"},{"DATA_JSON","CLOB"}},
            ["OASIS_HOLONS"]=new(){{"ID","RAW"},{"PARENT_HOLON_ID","RAW"},{"HOLON_TYPE","NUMBER"},{"IS_DELETED","NUMBER"},{"CREATED_DATE","TIMESTAMP"},{"MODIFIED_DATE","TIMESTAMP"},{"DATA_JSON","CLOB"}},
            ["OASIS_WRITE_RECEIPTS"]=new(){{"OPERATION_ID","RAW"},{"REQUEST_HASH","VARCHAR2"},{"RESULT_JSON","CLOB"},{"COMMITTED_AT","TIMESTAMP"}},
            ["OASIS_WRITE_OPERATION_LOCKS"]=new(){{"OPERATION_ID","RAW"}},
            ["OASIS_WRITE_PREPARATIONS"]=new(){{"OPERATION_ID","RAW"},{"ACTOR_ID","RAW"},{"PAYLOAD_JSON","CLOB"}}
        };

    private static async Task<bool> StorageSchemaExistsAsync(OracleConnection connection)
    {
        var names=StorageSchema.Keys.ToArray();
        var parameters=names.Select((_,index)=>":Table"+index);
        await using var command=CreateCommand("SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME IN ("+string.Join(",",parameters)+")",connection);
        for(var index=0;index<names.Length;index++)command.Parameters.Add("Table"+index,OracleDbType.Varchar2).Value=names[index];
        return Convert.ToInt32(await command.ExecuteScalarAsync())==names.Length;
    }

    private static async Task ValidateStorageSchemaAsync(OracleConnection connection)
    {
        var expected=StorageSchema;
        foreach(var table in expected){
            // Include user-declared invisible columns, not system-generated hidden columns.
            await using var command=CreateCommand("SELECT c.COLUMN_NAME,c.DATA_TYPE,c.DATA_LENGTH,c.NULLABLE,c.DATA_PRECISION,c.DATA_SCALE,c.CHAR_LENGTH,c.DEFAULT_LENGTH,c.CHAR_USED,t.TEMPORARY,t.DURATION,c.IDENTITY_COLUMN,c.VIRTUAL_COLUMN FROM USER_TAB_COLS c JOIN USER_TABLES t ON t.TABLE_NAME=c.TABLE_NAME WHERE c.TABLE_NAME=:TableName AND c.USER_GENERATED='YES'",connection);
            command.Parameters.Add("TableName",OracleDbType.Varchar2).Value=table.Key;
            var missing=new HashSet<string>(table.Value.Keys,StringComparer.Ordinal);
            await using var reader=await command.ExecuteReaderAsync();
            while(await reader.ReadAsync()){
                var column=reader.GetString(0);
                if(!table.Value.TryGetValue(column,out var required)){
                    // Canonical INSERTs omit extra columns. A required stored column
                    // without a default makes even an otherwise valid schema unwritable.
                    if(reader.GetString(3)=="N"&&(reader.IsDBNull(7)||reader.GetInt32(7)<=0)
                        &&reader.GetString(11)!="YES"&&reader.GetString(12)!="YES")
                        throw new InvalidDataException($"Oracle schema {table.Key}.{column} is an additional required column without an insert default or generated value.");
                    continue;
                }
                if(reader.IsDBNull(9)||reader.GetString(9)!="N"||!reader.IsDBNull(10))
                    throw new InvalidDataException($"Oracle schema {table.Key} requires a permanent table; temporary/session-scoped records are not durable provider storage.");
                ValidateIdentityCharacterSemantics(table.Key,column,reader.IsDBNull(8)?null:reader.GetString(8));
                ValidateStorageColumn(table.Key,column,required,reader.GetString(1),reader.GetInt32(2));
                ValidateStorageColumnProperties(table.Key,column,required,reader.GetString(3),
                    reader.IsDBNull(4)?null:reader.GetInt32(4),reader.IsDBNull(5)?null:reader.GetInt32(5),
                    reader.IsDBNull(6)?0:reader.GetInt32(6));
                ValidateStorageInsertDefault(table.Key,column,required,reader.IsDBNull(7)?null:reader.GetInt32(7));
                missing.Remove(column);
            }
            if(missing.Count!=0)throw new InvalidDataException($"Oracle schema {table.Key} is missing required columns: {string.Join(",",missing)}.");
        }
        foreach(var table in expected.Keys){
            var key=expected[table].ContainsKey("OPERATION_ID")?"OPERATION_ID":"ID";
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

    private static void ValidateIdentityCharacterSemantics(string table,string column,string? semantics)
    {
        if((table=="OASIS_AVATARS"||table=="OASIS_AVATAR_DETAILS")&&(column=="USERNAME"||column=="EMAIL")&&semantics!="C")
            throw new InvalidDataException($"Oracle schema {table}.{column} requires explicit character length semantics; review provisioning before activation. No automatic schema alteration is performed.");
    }

    private static void ValidateStorageInsertDefault(string table,string column,string required,int? defaultLength)
    {
        // The fixed canonical INSERTs and receipt claim omit these native audit timestamps.
        // All are NOT NULL; without a default activation would advertise an unwritable schema.
        if(required=="TIMESTAMP"&&defaultLength.GetValueOrDefault()<=0)
            throw new InvalidDataException($"Oracle schema {table}.{column} requires an insert default for its omitted audit timestamp.");
    }

    private static void ValidateStorageColumnProperties(string table,string column,string required,string nullable,
        int? precision,int? scale,int characters)
    {
        var permitsNull=column=="PARENT_HOLON_ID"||table=="OASIS_WRITE_RECEIPTS"&&column=="RESULT_JSON";
        if(nullable!=(permitsNull?"Y":"N"))
            throw new InvalidDataException($"Oracle schema {table}.{column} has incompatible nullability.");
        if(required=="VARCHAR2"&&characters<(column=="REQUEST_HASH"?64:256))
            throw new InvalidDataException($"Oracle schema {table}.{column} has insufficient character capacity.");
        // Unconstrained NUMBER can hold the native integer inputs. Negative scale rounds them;
        // fixed precision must leave enough integer digits after its fractional scale.
        if(required=="NUMBER"&&((scale??0)<0||(precision??38)-(scale??0)<(column=="HOLON_TYPE"?10:1)))
            throw new InvalidDataException($"Oracle schema {table}.{column} cannot preserve its native integer values.");
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
