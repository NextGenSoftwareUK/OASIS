using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS
{
    // SQLite cannot relax NOT NULL in place. Rebuild only this known child table in the activation transaction.
    private static async Task EnsureOptionalGiftChakraAsync(DbConnection connection,DbTransaction transaction)
    {
        using var command=connection.CreateCommand();command.Transaction=transaction;
        command.CommandText="PRAGMA table_info(\"AvatarGifts\")";
        var columns=new List<string>();var required=false;
        using(var reader=await command.ExecuteReaderAsync().ConfigureAwait(false))while(await reader.ReadAsync().ConfigureAwait(false)){
            var name=reader.GetString(1);columns.Add(name);if(name=="AvatarChakraId")required=reader.GetInt32(3)!=0;
        }
        if(!required)return;
        command.CommandText="SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        var tables=new List<string>();using(var reader=await command.ExecuteReaderAsync().ConfigureAwait(false))while(await reader.ReadAsync().ConfigureAwait(false))tables.Add(reader.GetString(0));
        foreach(var table in tables){
            command.CommandText="PRAGMA foreign_key_list("+QuoteGiftIdentifier(table)+")";
            using var reader=await command.ExecuteReaderAsync().ConfigureAwait(false);
            while(await reader.ReadAsync().ConfigureAwait(false))if(reader.GetString(2)=="AvatarGifts")
                throw new InvalidOperationException("Cannot rebuild gift table while another table references it; reviewed migration required.");
        }
        command.CommandText="SELECT count(*) FROM sqlite_master WHERE name='AvatarGifts_OptionalChakra'";
        if(Convert.ToInt64(await command.ExecuteScalarAsync().ConfigureAwait(false))!=0)throw new InvalidOperationException("Gift upgrade target already exists; reconcile it rather than overwriting.");
        command.CommandText="SELECT sql FROM sqlite_master WHERE type='table' AND name='AvatarGifts'";
        var original=(string)await command.ExecuteScalarAsync().ConfigureAwait(false);
        var columnPattern=new Regex("\"AvatarChakraId\"\\s+INTEGER\\s+NOT\\s+NULL",RegexOptions.CultureInvariant|RegexOptions.IgnoreCase);
        if(columnPattern.Matches(original).Count!=1)throw new InvalidOperationException("Unrecognized legacy gift chakra definition; refusing schema guessing.");
        var header=new Regex("^CREATE\\s+TABLE\\s+\"AvatarGifts\"",RegexOptions.CultureInvariant|RegexOptions.IgnoreCase);
        if(!header.IsMatch(original))throw new InvalidOperationException("Unrecognized legacy gift table header.");
        var replacement=header.Replace(columnPattern.Replace(original,"\"AvatarChakraId\" INTEGER NULL",1),"CREATE TABLE \"AvatarGifts_OptionalChakra\"",1);
        command.CommandText="SELECT sql FROM sqlite_master WHERE tbl_name='AvatarGifts' AND type IN ('index','trigger') AND sql IS NOT NULL";
        var objects=new List<string>();using(var reader=await command.ExecuteReaderAsync().ConfigureAwait(false))while(await reader.ReadAsync().ConfigureAwait(false))objects.Add(reader.GetString(0));
        command.CommandText="SELECT seq FROM sqlite_sequence WHERE name='AvatarGifts'";
        var oldSequence=await command.ExecuteScalarAsync().ConfigureAwait(false);
        command.CommandText=replacement;await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        var names=string.Join(",",columns.ConvertAll(QuoteGiftIdentifier));
        command.CommandText=$"INSERT INTO \"AvatarGifts_OptionalChakra\" ({names}) SELECT {names} FROM \"AvatarGifts\"";
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        command.CommandText="DROP TABLE \"AvatarGifts\"";await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        command.CommandText="ALTER TABLE \"AvatarGifts_OptionalChakra\" RENAME TO \"AvatarGifts\"";await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        foreach(var sql in objects){command.CommandText=sql;await command.ExecuteNonQueryAsync().ConfigureAwait(false);}
        if(oldSequence!=null && oldSequence!=DBNull.Value){
            command.CommandText="UPDATE sqlite_sequence SET seq=MAX(seq,$previous) WHERE name='AvatarGifts'";
            var parameter=command.CreateParameter();parameter.ParameterName="$previous";parameter.Value=oldSequence;command.Parameters.Add(parameter);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);command.Parameters.Clear();
        }
        command.CommandText="PRAGMA foreign_key_check(\"AvatarGifts\")";
        using(var reader=await command.ExecuteReaderAsync().ConfigureAwait(false))if(await reader.ReadAsync().ConfigureAwait(false))throw new InvalidOperationException("Gift schema upgrade failed native foreign-key validation.");
    }
    private static string QuoteGiftIdentifier(string value)=>"\""+value.Replace("\"","\"\"")+"\"";
}
