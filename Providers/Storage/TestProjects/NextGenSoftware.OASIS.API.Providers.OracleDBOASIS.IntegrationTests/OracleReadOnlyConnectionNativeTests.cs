using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public sealed class OracleReadOnlyConnectionNativeTests
{
    [TestMethod]
    public async Task CompareConnectionAdmissionWithoutReplayingOriginalWrites()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var connectionPath=Environment.GetEnvironmentVariable("OASIS_ORACLE_READ_CONNECTION_PATH")??"Provider";
        Assert.IsTrue(connectionPath is "Provider" or "Native","Explicit read-only connection path required.");
        var proof=Path.Combine(work,connectionPath=="Native"?"oracle-read-only-native-connection.json":"oracle-read-only-connection.json");Assert.IsFalse(File.Exists(proof));
        var intent=await File.ReadAllBytesAsync(Path.Combine(work,"read-acknowledgements.pending.json"));
        var failure=await File.ReadAllBytesAsync(Path.Combine(work,"read-acknowledgements-failure.json"));
        Assert.AreEqual("C83043A1A52487CB881ADE96B9038C328F78052CCC6DE126DCDB82D1043DA198",Convert.ToHexString(SHA256.HashData(intent)));
        Assert.AreEqual("470441EA9F0D9062305760949A92C303574FD668AF8CAD3369586B92E87F3F68",Convert.ToHexString(SHA256.HashData(failure)));
        using var evidence=JsonDocument.Parse(failure);
        var rows=evidence.RootElement.GetProperty("rows").EnumerateArray().ToArray();Assert.AreEqual(4,rows.Length);
        using var journal=JsonDocument.Parse(intent);
        var ids=journal.RootElement.GetProperty("ids").EnumerateArray().Select(x=>x.GetGuid()).ToArray();Assert.AreEqual(3,ids.Length);
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var iterationsText=Environment.GetEnvironmentVariable("OASIS_ORACLE_READ_CONNECTION_ITERATIONS")??"50";
        Assert.IsTrue(int.TryParse(iterationsText,out var iterations)&&iterations>=1&&iterations<=400,"Explicit bounded read-only workload required.");
        await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
        async Task VerifyOriginals()
        {
            foreach(var row in rows)
            {
                var table=row.GetProperty("table").GetString()!;
                Assert.IsTrue(new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS"}.Contains(table));
                var id=row.GetProperty("id").GetGuid();Assert.IsTrue(ids.Contains(id));
                var raw=await OracleNativeAcceptanceTests.Read(verifier,table,id);Assert.IsNotNull(raw);
                var bytes=Encoding.UTF8.GetBytes(raw);Assert.AreEqual(row.GetProperty("bytes").GetInt32(),bytes.Length);
                Assert.AreEqual(row.GetProperty("sha256").GetString(),Convert.ToHexString(SHA256.HashData(bytes)));
            }
        }
        async Task<long> Logons()
        {
            await using var command=new OracleCommand("SELECT VALUE FROM V$SYSSTAT WHERE NAME='logons cumulative'",verifier);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        await VerifyOriginals();var outcomes=new List<object>();
        // Native control opens its small SDK pool before applying unpooled churn;
        // a refusal induced by the latter must not contaminate pool initialization.
        foreach(var pooled in connectionPath=="Native"?new[]{true,false}:new[]{false,true})
        {
            var profile=new OracleConnectionStringBuilder(connection){Pooling=pooled,MinPoolSize=0,MaxPoolSize=4};
            var provider=new OracleDBOASIS(profile.ConnectionString);
            var failures=new List<object>();int attempted=0,bad=0;var logonsBefore=await Logons();var watch=Stopwatch.StartNew();
            void RecordFailure(bool isError,string? errorCode,Exception? exception)
            {
                bad++;if(failures.Count>=32)return;
                var chain=new List<object>();
                for(var error=exception;error!=null&&chain.Count<8;error=error.InnerException)
                    chain.Add(new{type=error.GetType().FullName,error.HResult,
                        oracleNumber=error is OracleException oracle?(int?)oracle.Number:null,
                        socketCode=error is System.Net.Sockets.SocketException socket?(int?)socket.SocketErrorCode:null});
                failures.Add(new{attempted,IsError=isError,ErrorCode=errorCode,exceptionChain=chain});
            }
            void Observe<T>(OASISResult<T> result,Guid expected)
            {
                attempted++;
                if(!result.IsError&&result.Result is IHolonBase record&&record.Id==expected)return;
                RecordFailure(result.IsError,result.ErrorCode,result.Exception);
            }
            void ObserveNative()
            {
                attempted++;
                try
                {
                    using var probe=new OracleConnection(profile.ConnectionString);probe.Open();
                    using var command=new OracleCommand("SELECT 1 FROM DUAL",probe){CommandTimeout=5};
                    if(Convert.ToInt32(command.ExecuteScalar())!=1)throw new InvalidDataException("Native read control returned an unexpected scalar.");
                }
                catch(Exception error){RecordFailure(true,null,error);}
            }
            for(int index=0;index<iterations;index++)
            {
                if(connectionPath=="Native")
                {
                    for(int read=0;read<8;read++)ObserveNative();
                    continue;
                }
                Observe(provider.LoadAvatar(ids[0]),ids[0]);Observe(provider.LoadAvatar(ids[0],1),ids[0]);
                Observe(provider.LoadAvatarDetail(ids[0]),ids[0]);Observe(provider.LoadAvatarDetail(ids[0],1),ids[0]);
                Observe(provider.LoadHolon(ids[1],loadChildren:false),ids[1]);Observe(provider.LoadHolon(ids[1],loadChildren:false,version:1),ids[1]);
                Observe(provider.LoadHolon(ids[2],loadChildren:false),ids[2]);Observe(provider.LoadHolon(ids[2],loadChildren:false,version:1),ids[2]);
            }
            watch.Stop();var logonsAfter=await Logons();await VerifyOriginals();
            outcomes.Add(new{pooled,attempted,bad,elapsedMilliseconds=watch.ElapsedMilliseconds,logonsBefore,logonsAfter,failures});
        }
        CollectionAssert.AreEqual(intent,await File.ReadAllBytesAsync(Path.Combine(work,"read-acknowledgements.pending.json")));
        CollectionAssert.AreEqual(failure,await File.ReadAllBytesAsync(Path.Combine(work,"read-acknowledgements-failure.json")));
        var bytesProof=JsonSerializer.SerializeToUtf8Bytes(new{schema=1,kind="read-only-connection-admission-control",connectionPath,iterations,originalsUnchanged=true,outcomes});
        using(var file=new FileStream(proof,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
        {await file.WriteAsync(bytesProof);file.Flush(true);}
        Console.WriteLine(Encoding.UTF8.GetString(bytesProof));
        // Passing records a completed observation, NOT provider-wide acceptance or a causal claim.
        // No activation, save, delete, retry, schema mutation or original-fixture cleanup is permitted here.
    }
}
