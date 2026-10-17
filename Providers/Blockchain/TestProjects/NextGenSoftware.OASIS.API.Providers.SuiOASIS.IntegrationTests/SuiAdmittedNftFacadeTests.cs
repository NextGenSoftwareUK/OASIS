using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class SuiAdmittedNftFacadeTests
{
    [TestMethod]
    public async Task AdmittedCSharpNftFacadeMintsReadsEscrowsTransfersAndBurns()
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var rpc=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC")!;Assert.IsTrue(new Uri(rpc).IsLoopback);
        var deploymentPath=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")!;
        using var deployment=JsonDocument.Parse(File.ReadAllText(deploymentPath));var settings=deployment.RootElement;
        using var retainedRecipient=JsonDocument.Parse(File.ReadAllText(deploymentPath+".token-wallet.json"));
        Assert.AreEqual(settings.GetProperty("chainId").GetString(),retainedRecipient.RootElement.GetProperty("chainId").GetString());
        var wallet=retainedRecipient.RootElement.GetProperty("wallet");var recipientKey=wallet.GetProperty("privateKey").GetString()!;
        SuiOASIS Provider(string key)=>new(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,
            key,settings.GetProperty("storageObjectId").GetString()!);
        using var owner=Provider(settings.GetProperty("privateKey").GetString()!);using var recipient=Provider(recipientKey);
        var ownerIdentity=await owner.RestoreKeyPairAsync(settings.GetProperty("privateKey").GetString()!);
        var recipientIdentity=await recipient.RestoreKeyPairAsync(recipientKey);
        Assert.IsFalse(ownerIdentity.IsError,ownerIdentity.Message);Assert.IsFalse(recipientIdentity.IsError,recipientIdentity.Message);
        Assert.AreEqual(wallet.GetProperty("address").GetString(),recipientIdentity.Result.WalletAddressLegacy);
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/Run-ReusableSuiV2Tests.ps1")))directory=directory.Parent;
        Assert.IsNotNull(directory);var journalPath=Path.Combine(directory!.FullName,"TestResults/SuiCurrent/csharp-nft-live-pending.jsonl");
        Assert.IsFalse(File.Exists(journalPath),"Retained facade ownership must be reconciled before rerun.");
        const string database="usage_sui_csharp_nft_live";var mongo=new MongoDB.Driver.MongoClient(connection);
        using(var names=await mongo.ListDatabaseNamesAsync())CollectionAssert.DoesNotContain(await MongoDB.Driver.IAsyncCursorExtensions.ToListAsync(names),database);
        using var journal=new FileStream(journalPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        void Record(object value){var bytes=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)+"\n");journal.Write(bytes);journal.Flush(true);}
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try {
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new SuiPreparedLedgerTests.TestAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://test-authority.invalid","WEB6","test-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions {Service="WEB6"})
                .BeginAsync("test-only",user,"sui-csharp-nft",new string('a',64),"POST /local-facade-test",new UsageEndpointPolicy {MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            Record(new {database,operationId=execution.OperationId,chainId=settings.GetProperty("chainId").GetString(),fixture="admitted CSharp NFT facade",recovery="Inspect original signed attempts; no automatic cleanup/replay on failed assertion."});
            async Task<OASISResult<IWeb3NFTTransactionResponse>> Owned(SuiOASIS provider,string signer,string stage,Func<Task<OASISResult<IWeb3NFTTransactionResponse>>> action){
                Record(new {stage,state="before-operation"});
                var result=await provider.ExecutePreparedNativeOperationAsync(action,execution,stage,signer,deadline.Token);
                Assert.IsFalse(result.IsError,result.Message);Assert.IsNotNull(result.Result);
                Record(new {stage,state="acknowledged",transactionId=result.Result.TransactionResult});return result;
            }
            var ownerAddress=ownerIdentity.Result.WalletAddressLegacy;var recipientAddress=recipientIdentity.Result.WalletAddressLegacy;
            var minted=await Owned(owner,ownerAddress,"mint",()=>owner.MintNFTAsync(new MintWeb3NFTRequest {
                Title="Admitted CSharp oak 🌳",Description="Retained local native facade fixture",NumberToMint=1,
                NFTStandardType=Core.Enums.NFTStandardType.SuiObject,StoreNFTMetaDataOnChain=true,
                MetaData=new Dictionary<string,string>{{"park","owned native facade"}},Price=12.5m,RoyaltyPercentage=7,
                SendToAddressAfterMinting=ownerAddress}));
            var nft=minted.Result.Web3NFT;Assert.IsFalse(string.IsNullOrWhiteSpace(nft.NFTTokenAddress));
            Record(new {tokenId=nft.NFTTokenAddress,metadataId=nft.Id});
            var read=await owner.LoadOnChainNFTDataAsync(nft.NFTTokenAddress);Assert.IsFalse(read.IsError,read.Message);
            Assert.AreEqual("Admitted CSharp oak 🌳",read.Result.Title);Assert.AreEqual("owned native facade",read.Result.MetaData["park"]);
            Assert.AreEqual(12.5m,read.Result.Price);Assert.AreEqual(700u,read.Result.SellerFeeBasisPoints);
            await Owned(owner,ownerAddress,"lock",()=>owner.LockNFTAsync(new LockWeb3NFTRequest {NFTTokenAddress=nft.NFTTokenAddress,Web3NFTId=nft.Id}));
            await Owned(owner,ownerAddress,"unlock",()=>owner.UnlockNFTAsync(new UnlockWeb3NFTRequest {NFTTokenAddress=nft.NFTTokenAddress,Web3NFTId=nft.Id}));
            var sent=await Owned(owner,ownerAddress,"send",()=>owner.SendNFTAsync(new SendWeb3NFTRequest {TokenId=nft.NFTTokenAddress,FromWalletAddress=ownerAddress,ToWalletAddress=recipientAddress,Amount=1}));
            Assert.AreEqual(recipientAddress,sent.Result.Web3NFT.SendToAddressAfterMinting);
            await Owned(recipient,recipientAddress,"burn",()=>recipient.BurnNFTAsync(new BurnWeb3NFTRequest {NFTTokenAddress=nft.NFTTokenAddress,
                OwnerPrivateKey=recipientKey,OwnerPublicKey=recipientIdentity.Result.PublicKey,OwnerSeedPhrase=""}));
            Assert.IsTrue((await owner.LoadOnChainNFTDataAsync(nft.NFTTokenAddress)).IsError);
            var retained=await store.GetAsync(execution.OperationId,deadline.Token);Assert.AreEqual(5,retained.ProviderAttempts.Count);
            CollectionAssert.AreEqual(new[]{"mint.storage.1","lock.storage.1","unlock.storage.1","send.storage.1","burn.storage.1"},retained.ProviderAttempts.Select(x=>x.ClientRequestId).ToArray());
            var receipts=new List<object>();
            foreach(var attempt in retained.ProviderAttempts){
                var fee=await owner.GetPreparedNativeFeeReceiptAsync(attempt,deadline.Token);Assert.IsFalse(fee.IsError,fee.Message);
                Assert.IsTrue(fee.Result.GetProperty("success").GetBoolean());Assert.AreEqual(attempt.ProviderRequestId,fee.Result.GetProperty("transactionHash").GetString());
                Assert.AreEqual(attempt.NativePreparation.Sender,fee.Result.GetProperty("payer").GetString());
                receipts.Add(new {attempt.ClientRequestId,attempt.ProviderRequestId,payer=attempt.NativePreparation.Sender,
                    chainId=attempt.NativePreparation.ChainId,netUnits=fee.Result.GetProperty("netUnits").GetString(),success=true});
            }
            var reportPath=Path.Combine(directory.FullName,"TestResults/SuiCurrent/sui-csharp-nft-receipts-current.json");
            using(var report=new FileStream(reportPath,FileMode.Create,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(report,new {operationId=execution.OperationId,nftTokenAddress=nft.NFTTokenAddress,
                    receipts,burned=true,productionBillingReconciled=false});report.Flush(true);
            }
            using(var report=JsonDocument.Parse(File.ReadAllBytes(reportPath)))Assert.AreEqual(5,report.RootElement.GetProperty("receipts").GetArrayLength());
            Console.WriteLine("Native CSharp facade: mint/read/lock/unlock/send/burn passed with5original signed attempts and bound native receipts; no external billing claim.");
            await mongo.DropDatabaseAsync(database);journal.Dispose();File.Delete(journalPath);
        } catch {Console.WriteLine("Retained exact facade journal/outbox for original-outcome reconciliation; no automatic mutation cleanup.");throw;}
    }
}
