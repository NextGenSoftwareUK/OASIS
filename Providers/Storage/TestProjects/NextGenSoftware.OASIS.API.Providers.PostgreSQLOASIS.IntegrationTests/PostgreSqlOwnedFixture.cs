using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests;

public partial class PostgreSqlNativeAcceptanceTests
{
    private class OwnedFixture:IAsyncDisposable
    {
        public readonly PostgreSQLOASIS Provider;
        public readonly NpgsqlConnection Connection;
        public readonly string Marker="owned-pg-record-"+Guid.NewGuid().ToString("N");
        private readonly string _journal;
        private readonly (string Table,Guid Id)[] _targets;
        private readonly Dictionary<(string Table,Guid Id),string> _observed=new();
        private bool _begun,_safe;
        public OwnedFixture(string name,IEnumerable<(string Table,Guid Id)> targets){
            var text=Environment.GetEnvironmentVariable("POSTGRESQLOASIS_CONNECTIONSTRING")!;
            var settings=new NpgsqlConnectionStringBuilder(text);Assert.AreEqual("127.0.0.1",settings.Host);Assert.AreEqual(15432,settings.Port);Assert.AreEqual("oasis_provider_test",settings.Database);
            _targets=targets.ToArray();Assert.IsTrue(_targets.Length>0);Assert.AreEqual(_targets.Length,_targets.Distinct().Count());
            Assert.IsTrue(_targets.All(target=>target.Id!=Guid.Empty&&new[]{"oasis_avatars","oasis_avatar_details","oasis_holons"}.Contains(target.Table)));
            _journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_POSTGRES_TEST_WORK")!,name+".pending.json");
            Provider=new PostgreSQLOASIS(text){IsVersionControlEnabled=true};Connection=new NpgsqlConnection(text);
        }
        public async Task Begin(){
            Assert.IsFalse(File.Exists(_journal));await Connection.OpenAsync();Assert.IsFalse((await Provider.ActivateProviderAsync()).IsError);
            foreach(var target in _targets)Assert.IsNull(await Body(target.Table,target.Id));
            using var file=new FileStream(_journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{targets=_targets.Select(target=>new{table=target.Table,id=target.Id}),marker=Marker,state="before-native-owned-fixture"}));file.Flush(true);_begun=true;
        }
        public void BeforeMutation()=>_safe=false;
        public async Task<string?> Body(string table,Guid id){
            Assert.IsTrue(_targets.Contains((table,id)));await using var read=new NpgsqlCommand($"SELECT data_json FROM {table} WHERE id=@id",Connection);read.Parameters.AddWithValue("id",id);return (string?)await read.ExecuteScalarAsync();
        }
        public async Task Observe(){
            foreach(var target in _targets){
                var body=await Body(target.Table,target.Id);if(body==null){_observed.Remove(target);continue;}
                using var json=JsonDocument.Parse(body);Assert.AreEqual(target.Id,json.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(Marker,json.RootElement.GetProperty("MetaData").GetProperty("owned-fixture").GetString());_observed[target]=body;
            }
            _safe=true;
        }
        public async ValueTask DisposeAsync(){
            Exception? failure=null;
            try{
                // Only known outcomes and exact observed synthetic payloads authorize cleanup.
                if(_begun&&_safe){
                    foreach(var row in _observed){await using var cleanup=new NpgsqlCommand($"DELETE FROM {row.Key.Table} WHERE id=@id AND data_json=@observed",Connection);cleanup.Parameters.AddWithValue("id",row.Key.Id);cleanup.Parameters.AddWithValue("observed",row.Value);Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}
                    foreach(var target in _targets)Assert.IsNull(await Body(target.Table,target.Id));File.Delete(_journal);
                }
            }catch(Exception cause){failure=cause;}
            try{await Provider.DeActivateProviderAsync();await Connection.DisposeAsync();}
            catch(Exception cause){failure=failure==null?cause:new AggregateException("Owned fixture cleanup and connection disposal failed.",failure,cause);}
            if(failure!=null)ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
