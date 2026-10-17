using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

public partial class HederaPreparationTests
{
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
            import {PrivateKey,AccountId,Client,Hbar} from '@hiero-ledger/sdk';
            import {pathToFileURL} from 'node:url';
            import {PreparedAccountInfoQuery} from './paid-query-preparation.mjs';
            const key=PrivateKey.generateED25519();process.stdout.write(JSON.stringify({publicKey:key.publicKey.toStringDer()})+'\n');
            let input='';for await(const chunk of process.stdin)input+=chunk;
            const config=JSON.parse(input);
            const {preparationSequenceClient}=await import(pathToFileURL(config.module));
            const sequence=preparationSequenceClient(config.endpoint,2,10000);
            const payer=AccountId.fromString('0.0.1001'),node=AccountId.fromString('0.0.3');
            const client=Client.forNetwork({'127.0.0.1:50211':node});client.setOperator(payer,key);
            let transports=0;
            try{
              for(let index=0;index<2;index++){
                const query=new PreparedAccountInfoQuery({network:'testnet',payer:payer.toString(),publicKey:key.publicKey.toStringDer(),nodes:[node.toString()],
                  maxPaymentTinybars:'123',maxTransactionFeeTinybars:'100000000',persistenceTimeoutMs:10000,persist:sequence.persist})
                  .setAccountId(payer).setNodeAccountIds([node]).setQueryPayment(Hbar.fromTinybars('123'));
                await query._beforeExecute(client);const request=await query._makeRequestAsync();
                try{await query._executeRequestWithGrpcDeadline({crypto:{getAccountInfo:async()=>{transports++;throw new Error('controlled transport stop');}}},request);}
                catch(error){if(error.message!=='controlled transport stop')throw error;}
              }
              await sequence.complete();process.stdout.write(JSON.stringify({transports})+'\n');
            }finally{client.close();}
            """);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(25));
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
            CollectionAssert.AreEqual(new[]{"paid.query.1","paid.query.2"},stored.ProviderAttempts.Select(attempt=>attempt.ClientRequestId).ToArray());
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
        }finally{
            if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}
            await mongo.DropDatabaseAsync(database);
        }
    }
}
