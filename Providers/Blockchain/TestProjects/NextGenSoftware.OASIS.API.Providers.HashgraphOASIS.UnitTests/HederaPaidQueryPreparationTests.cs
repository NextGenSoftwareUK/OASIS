using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

public partial class HederaPreparationTests
{
    [DataTestMethod]
    [DataRow("fileRead")] [DataRow("transfer")]
    public async Task ProcessSuccessWithoutOwnedQueryCompletionCannotClaimTrackedSuccess(string action)
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var database="usage_hedera_query_process_"+Guid.NewGuid().ToString("N");var mongo=new MongoDB.Driver.MongoClient(connection);
        var previous=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try{
            Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","query-missing-completion.mjs"));
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only-bearer",user,"query-process",new string('a',64),"POST /fixture",new(){MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            var provider=new HashgraphOASIS(network:"testnet",chainId:"296");
            var operation=provider.ExecutePreparedWithQueriesAsync(action,new {network="testnet",sender="0.0.1001",fileId="0.0.2001",
                transactionId="0.0.1001@1700000000.000000001"},execution,"process","0.0.1001","controlled-public-authority",
                new[]{"0.0.3"},123,100000000,maximumRequests:2,token:deadline.Token);
            if(action=="transfer")await Assert.ThrowsExceptionAsync<HederaNativeMutationReconciliationException>(async()=>await operation);
            else await Assert.ThrowsExceptionAsync<InvalidDataException>(async()=>await operation);
            var retained=await store.GetAsync(execution.OperationId,deadline.Token);Assert.AreEqual(0,retained.Receipts.Count);Assert.IsNull(retained.Settlement);
        }finally{Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",previous);await mongo.DropDatabaseAsync(database);}
    }

    [TestMethod]
    public async Task ActualBridgeFileReadOwnsPaidSdkGrpcRequestAndCompletesDurableQueryStream()
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var bridge=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE")!;
        var start=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,
            RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(bridge)};
        start.ArgumentList.Add("--input-type=module");start.ArgumentList.Add("-e");
        start.ArgumentList.Add("""
            import grpc from '@grpc/grpc-js';import {proto} from '@hiero-ledger/proto';
            import {PrivateKey,Transaction} from '@hiero-ledger/sdk';
            const key=PrivateKey.fromStringED25519('11'.repeat(32));
            const server=new grpc.Server();
            server.addService({getFileContent:{path:'/proto.FileService/getFileContent',requestStream:false,responseStream:false,
              requestSerialize:value=>Buffer.from(proto.Query.encode(value).finish()),requestDeserialize:bytes=>proto.Query.decode(bytes),
              responseSerialize:value=>Buffer.from(proto.Response.encode(value).finish()),responseDeserialize:bytes=>proto.Response.decode(bytes)}},
              {getFileContent:(call,callback)=>{
                const query=call.request.fileGetContents;const header=query.header;
                if(header.responseType===proto.ResponseType.ANSWER_ONLY){
                  const payment=Transaction.fromBytes(proto.Transaction.encode(header.payment).finish());
                  process.stdout.write(JSON.stringify({paymentId:payment.transactionId.toString()})+'\n');
                }
                callback(null,{fileGetContents:{header:{nodeTransactionPrecheckCode:proto.ResponseCodeEnum.OK,responseType:header.responseType,cost:123},
                  fileContents:{fileID:query.fileID,contents:Buffer.from('controlled native protocol read')}}});
              }});
            const port=await new Promise((resolve,reject)=>server.bindAsync('127.0.0.1:0',grpc.ServerCredentials.createInsecure(),(error,port)=>error?reject(error):resolve(port)));
            process.stdout.write(JSON.stringify({port,publicKey:key.publicKey.toStringDer()})+'\n');
            for await(const chunk of process.stdin){}server.forceShutdown();
            """);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(35));
        using var child=Process.Start(start)??throw new InvalidOperationException("Cannot start owned SDK protocol fixture.");
        var errors=child.StandardError.ReadToEndAsync(deadline.Token);
        var database="usage_hedera_query_grpc_"+Guid.NewGuid().ToString("N");var mongo=new MongoDB.Driver.MongoClient(connection);
        try{
            using var initial=JsonDocument.Parse(await child.StandardOutput.ReadLineAsync(deadline.Token)??throw new InvalidDataException("Missing fixture endpoint."));
            var port=initial.RootElement.GetProperty("port").GetInt32();var publicKey=initial.RootElement.GetProperty("publicKey").GetString()!;
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only-bearer",user,"query-grpc",new string('a',64),"POST /fixture",new(){MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            var provider=new HashgraphOASIS(network:"local",rpcEndpoint:$"http://127.0.0.1:{port}/api/v1",consensusNodes:new Dictionary<string,string>{{"127.0.0.1:"+port,"0.0.3"}});
            var operation=provider.ExecutePreparedWithQueriesAsync("fileRead",new {network="local",nodes=new Dictionary<string,string>{{"127.0.0.1:"+port,"0.0.3"}},
                sender="0.0.1001",privateKey=new string('1',64),keyType="ed25519",fileId="0.0.2001",maxFeeTinybars="1",
                queryMaxPaymentTinybars="999999999999",queryMaximumRequests=512},execution,new string('p',128),"0.0.1001",publicKey,new[]{"0.0.3"},135,100000000,maximumRequests:2,token:deadline.Token);
            var paymentEvent=child.StandardOutput.ReadLineAsync(deadline.Token).AsTask();
            if(await Task.WhenAny(paymentEvent,operation)==operation)await operation;
            using var eventDocument=JsonDocument.Parse(await paymentEvent??throw new InvalidDataException("No selected payment reached SDK transport."));
            var beforeReply=await store.GetAsync(execution.OperationId,deadline.Token);Assert.AreEqual(1,beforeReply.ProviderAttempts.Count);
            Assert.AreEqual(eventDocument.RootElement.GetProperty("paymentId").GetString(),beforeReply.ProviderAttempts[0].ProviderRequestId);
            var response=await operation;Assert.AreEqual("controlled native protocol read",response.GetProperty("contents").GetString());
            var context=JsonDocument.Parse(Convert.FromBase64String(beforeReply.ProviderAttempts[0].NativePreparation.SigningAuthority.EvidenceBase64));
            using(context){Assert.AreEqual("135",context.RootElement.GetProperty("MaxPaymentTinybars").GetString());
                Assert.AreEqual("135",context.RootElement.GetProperty("PaymentTinybars").GetString());}
            Assert.IsTrue(beforeReply.ProviderAttempts[0].ClientRequestId.Length<=128);Assert.AreEqual(0,beforeReply.Receipts.Count);
            child.StandardInput.Close();await child.WaitForExitAsync(deadline.Token);Assert.AreEqual(0,child.ExitCode,await errors);
        }finally{if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}await mongo.DropDatabaseAsync(database);}
    }

    [TestMethod]
    public async Task SelectedSdkQueryPaymentsPersistThroughRealPipeAndOwnedMongoBeforeTransport()
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var bridge=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE")!;
        var module=Path.Combine(AppContext.BaseDirectory,"HederaSdkBridge","prepared-submission.mjs");
        var start=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,
            RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(bridge)};
        start.ArgumentList.Add("--input-type=module");start.ArgumentList.Add("-e");
        start.ArgumentList.Add("""
            import {PrivateKey,AccountId,Client,Hbar,Transaction} from '@hiero-ledger/sdk';
            import {pathToFileURL} from 'node:url';
            import {PreparedAccountInfoQuery} from './paid-query-preparation.mjs';
            const key=PrivateKey.generateED25519();process.stdout.write(JSON.stringify({publicKey:key.publicKey.toStringDer()})+'\n');
            let input='';for await(const chunk of process.stdin)input+=chunk;
            const config=JSON.parse(input);
            const {preparationSequenceClient}=await import(pathToFileURL(config.module));
            const sequence=preparationSequenceClient(config.endpoint,2,10000);
            const payer=AccountId.fromString('0.0.1001'),node=AccountId.fromString('0.0.3');
            const client=Client.forNetwork({'127.0.0.1:50211':node});client.setOperator(payer,key);
            let transports=0;const hashes=[];
            try{
              for(let index=0;index<2;index++){
                const query=new PreparedAccountInfoQuery({network:'testnet',payer:payer.toString(),publicKey:key.publicKey.toStringDer(),nodes:[node.toString()],
                  maxPaymentTinybars:'123',maxTransactionFeeTinybars:'100000000',persistenceTimeoutMs:10000,persist:async evidence=>{
                    await sequence.persist(evidence);hashes.push({id:evidence.transactionHash,hash:Buffer.from(await Transaction.fromBytes(Buffer.from(evidence.signedTransactionBase64,'base64')).getTransactionHash()).toString('base64')});
                  }})
                  .setAccountId(payer).setNodeAccountIds([node]).setQueryPayment(Hbar.fromTinybars('123'));
                await query._beforeExecute(client);const request=await query._makeRequestAsync();
                try{await query._executeRequestWithGrpcDeadline({crypto:{getAccountInfo:async()=>{transports++;throw new Error('controlled transport stop');}}},request);}
                catch(error){if(error.message!=='controlled transport stop')throw error;}
              }
              await sequence.complete();process.stdout.write(JSON.stringify({transports,hashes})+'\n');
            }finally{client.close();}
            """);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var child=Process.Start(start)??throw new InvalidOperationException("Cannot start retained SDK query fixture.");
        var errors=child.StandardError.ReadToEndAsync(deadline.Token);
        var database="usage_hedera_paid_query_"+Guid.NewGuid().ToString("N");var mongo=new MongoDB.Driver.MongoClient(connection);
        try{
            using var initial=JsonDocument.Parse(await child.StandardOutput.ReadLineAsync(deadline.Token)??throw new InvalidDataException("Missing public authority."));
            var publicKey=initial.RootElement.GetProperty("publicKey").GetString()!;
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
            var initialExecution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only-bearer",user,"query-fixture",new string('a',64),"POST /fixture",new(){MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            var now=DateTime.UtcNow.AddSeconds(-1);
            var quote=new NativeFeePriceQuote(Guid.Parse(initialExecution.OperationId),"HashgraphOASIS","testnet","tinybar","hedera-hashgraph",8,1m,
                "controlled-source","fixture-v1","fixture-v1",now,now,now,now.AddMinutes(5));
            var execution=initialExecution.WithNativePrices(new[]{quote});
            var provider=new HashgraphOASIS(network:"testnet",chainId:"296");var nodes=new[]{"0.0.3"};
            using var server=provider.CreatePaidQueryPreparationServer(execution,"paid","0.0.1001",publicKey,nodes,123,100000000,2);
            nodes[0]="0.0.4"; // Caller mutation cannot replace the independent admitted node snapshot.
            var serving=server.ServeAsync(deadline.Token);
            await child.StandardInput.WriteAsync(JsonSerializer.Serialize(new {endpoint=server.Endpoint,module}));child.StandardInput.Close();
            var report=await child.StandardOutput.ReadToEndAsync(deadline.Token);await child.WaitForExitAsync(deadline.Token);await serving;
            Assert.AreEqual(0,child.ExitCode,await errors);Assert.IsTrue(server.SequenceCompleted);
            using var result=JsonDocument.Parse(report);Assert.AreEqual(2,result.RootElement.GetProperty("transports").GetInt32());
            var stored=await new MongoUsageOutboxStore(connection,database).GetAsync(execution.OperationId,deadline.Token);
            var prefix="query."+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("paid"))).ToLowerInvariant();
            CollectionAssert.AreEqual(new[]{prefix+".1",prefix+".2"},stored.ProviderAttempts.Select(attempt=>attempt.ClientRequestId).ToArray());
            Assert.AreEqual(2,stored.ProviderAttempts.Select(attempt=>attempt.ProviderRequestId).Distinct().Count());
            Assert.AreEqual(0,stored.Receipts.Count,"Admission is not a fee receipt or a zero-cost settlement.");
            foreach(var attempt in stored.ProviderAttempts){
                Assert.AreEqual("hedera-query-payment-v1",attempt.NativePreparation.SigningAuthority.Scheme);
                using var context=JsonDocument.Parse(Convert.FromBase64String(attempt.NativePreparation.SigningAuthority.EvidenceBase64));
                Assert.AreEqual(publicKey,context.RootElement.GetProperty("PublicKey").GetString());
                Assert.AreEqual("123",context.RootElement.GetProperty("PaymentTinybars").GetString());
                Assert.AreEqual("0.0.3",context.RootElement.GetProperty("NodeAccountId").GetString());
                Assert.AreEqual(quote,attempt.NativePreparation.PriceQuotes.Single());Assert.IsNull(attempt.AcknowledgedAtUtc);
            }
            var mirror=new QueryPaymentMirror();
            foreach(var hash in result.RootElement.GetProperty("hashes").EnumerateArray())mirror.Hashes.Add(hash.GetProperty("id").GetString()!,hash.GetProperty("hash").GetString()!);
            using var mirrorHttp=new HttpClient(mirror){BaseAddress=new Uri("https://testnet.mirrornode.hedera.com/api/v1/")};
            var httpField=typeof(HashgraphOASIS).GetField("_httpClient",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            ((HttpClient)httpField.GetValue(provider)!).Dispose();httpField.SetValue(provider,mirrorHttp);
            var original=stored.ProviderAttempts[0];
            foreach(var mode in new[]{"success","duplicate","failed","missing-transfers","wrong-debit","wrong-hash","foreign-id"}){
                mirror.Mode=mode;
                var fee=await provider.GetPreparedNativeFeeReceiptAsync(original,deadline.Token);
                if(mode is "success" or "duplicate" or "failed"){
                    Assert.IsFalse(fee.IsError,fee.Message);
                    Assert.AreEqual(mode=="failed"?"5":mode=="duplicate"?"135":"128",fee.Result.GetProperty("netUnits").GetString());
                    Assert.AreEqual(mode=="duplicate"?"12":"5",fee.Result.GetProperty("networkFeeUnits").GetString());
                    Assert.AreEqual(mode=="failed"?"0":"123",fee.Result.GetProperty("queryPaymentUnits").GetString());
                    Assert.AreEqual(mode!="failed",fee.Result.GetProperty("success").GetBoolean());
                }else{Assert.IsTrue(fee.IsError,mode);Assert.AreEqual(JsonValueKind.Undefined,fee.Result.ValueKind);}
            }
            mirror.Mode="success";
            var queryAuthority=original.NativePreparation.SigningAuthority;
            try{
                original.NativePreparation.SigningAuthority=new("hedera-payer-public-key-v1",Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(publicKey)));
                var ordinary=await provider.GetPreparedNativeFeeReceiptAsync(original,deadline.Token);
                Assert.IsFalse(ordinary.IsError,ordinary.Message);Assert.AreEqual("5",ordinary.Result.GetProperty("netUnits").GetString(),
                    "An ordinary user transfer must not relabel its principal as a service query fee.");
            }finally{original.NativePreparation.SigningAuthority=queryAuthority;}
            var previousCulture=System.Globalization.CultureInfo.CurrentCulture;
            try{
                var custom=(System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();custom.NumberFormat.NegativeSign="~";
                System.Globalization.CultureInfo.CurrentCulture=custom;
                var invariant=await provider.GetPreparedNativeFeeReceiptAsync(original,deadline.Token);
                Assert.IsFalse(invariant.IsError,invariant.Message);Assert.AreEqual("128",invariant.Result.GetProperty("netUnits").GetString());
            }finally{System.Globalization.CultureInfo.CurrentCulture=previousCulture;}
            var reader=(NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader)provider;
            Assert.AreEqual("HashgraphOASIS",reader.NativeFeeProvider);Assert.AreEqual("testnet",reader.NativeFeeChainId);
            Assert.AreEqual(NativeFeeCoverage.NetFinal,reader.DeclaredFeeCoverage);
            mirror.Mode="wrong-debit";
            var rejected=await reader.ReadPreparedNativeFeeEvidenceAsync(original,deadline.Token);
            Assert.IsTrue(rejected.IsError);Assert.IsNull(rejected.Result,"Unverified query debit cannot become typed final evidence.");
            var recovery=new PreparedNativeUsageRecoveryResolver(store,new[]{reader});
            Assert.IsTrue(recovery.CanResolve(stored));
            await Assert.ThrowsExceptionAsync<UsageProtocolException>(()=>recovery.ResolveAsync(stored,deadline.Token));
            Assert.AreEqual(0,(await store.GetAsync(execution.OperationId,deadline.Token)).Receipts.Count);
            mirror.Mode="success";
            Assert.IsNull(await recovery.ResolveAsync(stored,deadline.Token),"Native success is not an observed API outcome.");
            var recovered=await store.GetAsync(execution.OperationId,deadline.Token);
            Assert.AreEqual(2,recovered.Receipts.Count);
            foreach(var receipt in recovered.Receipts){
                Assert.AreEqual(0.00000128m,receipt.ActualCostUsd);
                Assert.AreEqual("128",receipt.NativeFeeValuations.Single().NativeNetUnits);
            }
            var queriesAfterRecovery=mirror.Requests;
            await store.RecordObservedOutcomeAsync(recovered.Id,recovered.ExecutionOwner,"failed",deadline.Token);
            var settlement=await recovery.ResolveAsync(await store.GetAsync(execution.OperationId,deadline.Token),deadline.Token);
            Assert.IsNotNull(settlement);Assert.AreEqual("failed",settlement.Outcome);Assert.AreEqual(0.00000256m,settlement.ActualCostUsd);
            Assert.AreEqual(queriesAfterRecovery,mirror.Requests,"Retained receipts must prevent repeated mirror lookups.");
        }finally{
            if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}
            await mongo.DropDatabaseAsync(database);
        }
    }

    private sealed class QueryPaymentMirror:HttpMessageHandler
    {
        public readonly Dictionary<string,string> Hashes=new();
        public string Mode="success";
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();Requests++;
            Assert.AreEqual("?nonce=0&scheduled=false",request.RequestUri!.Query);
            var pair=Hashes.Single(entry=>{
                var parts=entry.Key.Split('@');var time=parts[1].Split('.');
                return request.RequestUri.AbsolutePath.EndsWith(parts[0]+"-"+time[0]+"-"+time[1],StringComparison.Ordinal);
            });
            var id=pair.Key.Split('@');var start=id[1].Split('.');var mirrorId=id[0]+"-"+start[0]+"-"+start[1];
            Dictionary<string,object?> Row(string status,long fee,long debit,string timestamp){
                var row=new Dictionary<string,object?> { ["transaction_id"]=Mode=="foreign-id"?"0.0.9999-"+start[0]+"-"+start[1]:mirrorId,
                    ["transaction_hash"]=Mode=="wrong-hash"?Convert.ToBase64String(new byte[48]):pair.Value,["nonce"]=0,["scheduled"]=false,
                    ["parent_consensus_timestamp"]=null,["name"]="CRYPTOTRANSFER",["result"]=status,["consensus_timestamp"]=timestamp,["charged_tx_fee"]=fee};
                if(Mode!="missing-transfers")row["transfers"]=new[]{new {account="0.0.1001",amount=-debit,is_approval=false},new {account="0.0.3",amount=debit,is_approval=false}};
                return row;
            }
            var rows=new List<Dictionary<string,object?>> {Row(Mode=="failed"?"INSUFFICIENT_TX_FEE":"SUCCESS",5,Mode=="failed"?5:Mode=="wrong-debit"?127:128,"1700000001.000000001")};
            if(Mode=="duplicate")rows.Add(Row("DUPLICATE_TRANSACTION",7,7,"1700000002.000000001"));
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(new {transactions=rows}))});
        }
    }
}
