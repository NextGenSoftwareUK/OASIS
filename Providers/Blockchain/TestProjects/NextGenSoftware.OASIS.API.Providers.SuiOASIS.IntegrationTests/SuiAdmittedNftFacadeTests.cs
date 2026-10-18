using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class SuiAdmittedNftFacadeTests
{
    [TestMethod]
    public async Task AdmittedCSharpNftFacadeMintsReadsEscrowsTransfersAndBurns()
        =>await RunNativeFacade(false);
    [TestMethod]
    public async Task AdmittedCSharpTokenFacadeBalancesEscrowsPartialBurnAndSupply()
        =>await RunNativeFacade(true);
    [TestMethod]
    public async Task AdmittedCSharpIdBoundTokenFacadeUsesEncryptedWalletAndOriginalReceipts()
        =>await RunNativeFacade(true,idBound:true);
    [TestMethod]
    public async Task AdmittedCSharpNftBatchReturnsDistinctObjectsAndCleansEachOwner()
        =>await RunNativeFacade(false,2);
    [TestMethod]
    public async Task AdmittedCSharpOffChainNftKeepsMetadataOutOfNativeObject()
        =>await RunNativeFacade(false,1,true);
    private async Task RunNativeFacade(bool token,int nftCount=1,bool offChain=false,bool idBound=false)
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var rpc=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC")!;Assert.IsTrue(new Uri(rpc).IsLoopback);
        var deploymentPath=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")!;
        using var deployment=JsonDocument.Parse(File.ReadAllText(deploymentPath));var settings=deployment.RootElement;
        var ownerKey=settings.GetProperty("privateKey").GetString()!;
        using var escrowDeployment=idBound?JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_ESCROW_CACHE")
            ??throw new InvalidOperationException("Retained reviewed escrow deployment required; no publication allowed here."))):null;
        if(idBound){Assert.IsTrue(escrowDeployment!.RootElement.GetProperty("completed").GetBoolean());
            Assert.AreEqual(settings.GetProperty("chainId").GetString(),escrowDeployment.RootElement.GetProperty("chainId").GetString());settings=escrowDeployment.RootElement;}
        using var retainedRecipient=JsonDocument.Parse(File.ReadAllText(deploymentPath+".token-wallet.json"));
        Assert.AreEqual(settings.GetProperty("chainId").GetString(),retainedRecipient.RootElement.GetProperty("chainId").GetString());
        var wallet=retainedRecipient.RootElement.GetProperty("wallet");var recipientKey=wallet.GetProperty("privateKey").GetString()!;
        SuiOASIS Provider(string key)=>new(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,
            key,settings.GetProperty("storageObjectId").GetString()!,settings.GetProperty("currencyObjectId").GetString()!,
            tokenEscrow:idBound?new(settings.GetProperty("escrowObjectId").GetString()!):null);
        using var owner=Provider(ownerKey);using var recipient=Provider(recipientKey);
        var ownerIdentity=await owner.RestoreKeyPairAsync(ownerKey);
        var recipientIdentity=await recipient.RestoreKeyPairAsync(recipientKey);
        Assert.IsFalse(ownerIdentity.IsError,ownerIdentity.Message);Assert.IsFalse(recipientIdentity.IsError,recipientIdentity.Message);
        Assert.AreEqual(wallet.GetProperty("address").GetString(),recipientIdentity.Result.WalletAddressLegacy);
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/Run-ReusableSuiV2Tests.ps1")))directory=directory.Parent;
        var assetKind=idBound?"escrow":token?"token":"nft";
        Assert.IsNotNull(directory);var journalPath=Path.Combine(directory!.FullName,$"TestResults/SuiCurrent/csharp-{assetKind}-live-pending.jsonl");
        Assert.IsFalse(File.Exists(journalPath),"Retained facade ownership must be reconciled before rerun.");
        var scenario=token?assetKind:offChain?"nft-offchain":nftCount>1?"nft-batch":assetKind;
        var reportPath=Path.Combine(directory.FullName,$"TestResults/SuiCurrent/sui-csharp-{scenario}-receipts-current.json");
        var depositGuid=Guid.Parse("33333333-4444-5555-6666-777777777777");
        var depositId=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(depositGuid.ToString("N")))).ToLowerInvariant();
        var sdk=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        Task<JsonElement> ReadSdk(string action,object? args=null)=>(Task<JsonElement>)sdk.Invoke(owner,new object[]{action,args!,CancellationToken.None})!;
        if(idBound&&File.Exists(reportPath)){
            using var prior=JsonDocument.Parse(File.ReadAllText(reportPath));Assert.IsTrue(prior.RootElement.GetProperty("burned").GetBoolean());
            Assert.AreEqual(settings.GetProperty("coinType").GetString(),prior.RootElement.GetProperty("assetId").GetString());
            var saved=await ReadSdk("tokenEscrowGet",new{depositId,escrowObjectId=settings.GetProperty("escrowObjectId").GetString()});
            Assert.IsTrue(saved.GetProperty("released").GetBoolean());Assert.AreEqual("0",saved.GetProperty("funds").GetString());
            Assert.AreEqual("123",saved.GetProperty("amount").GetString());Assert.AreEqual(ownerIdentity.Result.WalletAddressLegacy,saved.GetProperty("owner").GetString());
            Assert.AreEqual("0",(await ReadSdk("tokenProbe")).GetProperty("treasury").GetProperty("supply").GetString());
            var retainedBalance=await owner.GetTokenBalanceAsync(ownerIdentity.Result.WalletAddressLegacy,settings.GetProperty("coinType").GetString()!,true);
            Assert.IsFalse(retainedBalance.IsError,retainedBalance.Message);Assert.AreEqual(0m,retainedBalance.Result);
            var originals=prior.RootElement.GetProperty("receipts").EnumerateArray().ToArray();
            Assert.AreEqual(4,originals.Length);
            CollectionAssert.AreEqual(new[]{"mint.storage.1","lock.storage.1","unlock.storage.1","burn.storage.1"},
                originals.Select(row=>row.GetProperty("ClientRequestId").GetString()).ToArray());
            Assert.AreEqual(4,originals.Select(row=>row.GetProperty("ProviderRequestId").GetString()).Distinct().Count());
            // Read retained ledger history through both public overloads; never mint new fixtures here.
            var request=new GetWeb3TransactionsRequest{WalletAddress=ownerIdentity.Result.WalletAddressLegacy};
            var asyncHistory=await owner.GetTransactionsAsync(request);Assert.IsFalse(asyncHistory.IsError,asyncHistory.Message);
            var syncHistory=owner.GetTransactions(request);Assert.IsFalse(syncHistory.IsError,syncHistory.Message);
            foreach(var original in originals){
                Assert.IsTrue(original.GetProperty("success").GetBoolean());
                Assert.AreEqual(settings.GetProperty("chainId").GetString(),original.GetProperty("chainId").GetString());
                Assert.AreEqual(ownerIdentity.Result.WalletAddressLegacy,original.GetProperty("payer").GetString());
                var digest=original.GetProperty("ProviderRequestId").GetString()!;
                var fee=await owner.GetNativeFeeReceiptAsync(digest);Assert.IsFalse(fee.IsError,fee.Message);
                Assert.IsTrue(fee.Result.GetProperty("success").GetBoolean());
                Assert.AreEqual(digest,fee.Result.GetProperty("transactionHash").GetString());
                Assert.AreEqual(original.GetProperty("payer").GetString(),fee.Result.GetProperty("payer").GetString());
                Assert.AreEqual(original.GetProperty("netUnits").GetString(),fee.Result.GetProperty("netUnits").GetString());
                var status=await owner.GetTransactionStatusAsync(digest);Assert.IsFalse(status.IsError,status.Message);
                Assert.AreEqual(Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,status.Result);
                foreach(var history in new[]{asyncHistory.Result,syncHistory.Result}){
                    var gasRows=history.Where(row=>row.NativeTransactionId==digest
                        && row.AmountUnit.EndsWith("::sui::SUI:base-unit",StringComparison.Ordinal)).ToArray();
                    Assert.AreEqual(1,gasRows.Length,"Original escrow gas must appear exactly once in wallet history.");
                    var gas=gasRows[0];Assert.AreEqual("Success",gas.NativeExecutionResult);
                    Assert.AreEqual("WalletNetBalanceChange",gas.NativeOperation);
                    var netFee=System.Numerics.BigInteger.Parse(fee.Result.GetProperty("netUnits").GetString()!,System.Globalization.CultureInfo.InvariantCulture);
                    Assert.AreEqual((-netFee).ToString(System.Globalization.CultureInfo.InvariantCulture),gas.RawAmount);
                    Assert.AreEqual(ownerIdentity.Result.WalletAddressLegacy,gas.FromWalletAddress);
                    if(netFee.Sign>0){Assert.IsNull(gas.ToWalletAddress,"Gas debit must not invent a recipient.");
                        Assert.AreEqual(Core.Enums.TransactionType.Debit,gas.TransactionType);}
                    else {Assert.AreEqual(ownerIdentity.Result.WalletAddressLegacy,gas.ToWalletAddress);
                        Assert.AreEqual(Core.Enums.TransactionType.Credit,gas.TransactionType);}
                }
            }
            Console.WriteLine("Retained CSharp escrow state/all4original native fee receipts/status and both sync/async wallet history rechecked read-only; no signed mutation.");return;
        }
        var database=idBound?"usage_sui_csharp_escrow_live":token?"usage_sui_csharp_token_live":"usage_sui_csharp_nft_live";var mongo=new MongoDB.Driver.MongoClient(connection);
        using(var names=await mongo.ListDatabaseNamesAsync())CollectionAssert.DoesNotContain(await MongoDB.Driver.IAsyncCursorExtensions.ToListAsync(names),database);
        using var journal=new FileStream(journalPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        void Record(object value){var bytes=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)+"\n");journal.Write(bytes);journal.Flush(true);}
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(90));
        API.Providers.LocalFileOASIS.LocalFileOASIS? walletStorage=null;FileStream? walletLease=null;
        var walletAvatar=Guid.Parse("478e4ef2-b442-459f-93e0-fec53c7de2f1");string? walletFile=null,encrypted=null;
        Core.Managers.WalletManager? walletManager=null;
        try {
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new SuiPreparedLedgerTests.TestAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://test-authority.invalid","WEB6","test-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions {Service="WEB6"})
                .BeginAsync("test-only",user,"sui-csharp-"+assetKind,new string('a',64),"POST /local-facade-test",new UsageEndpointPolicy {MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            Record(new {database,operationId=execution.OperationId,chainId=settings.GetProperty("chainId").GetString(),assetKind,fixture="admitted CSharp facade",recovery="Inspect original signed attempts; no automatic cleanup/replay on failed assertion."});
            if(idBound){
                var walletRoot=Path.Combine(directory.FullName,"TestResults/HederaCurrent/wallet-failure-files");Directory.CreateDirectory(walletRoot);
                walletLease=new FileStream(Path.Combine(walletRoot,"fixture.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
                walletFile=Path.GetFullPath(Path.Combine(walletRoot,"wallets_"+walletAvatar+".json"));Assert.IsFalse(File.Exists(walletFile));
                walletStorage=new(filePath:walletRoot+Path.DirectorySeparatorChar,storageDirectory:walletRoot);Assert.IsFalse((await walletStorage.ActivateProviderAsync()).IsError);
                encrypted=Rijndael256.Rijndael.Encrypt(ownerKey,new string('k',32),Rijndael256.KeySize.Aes256);
                Assert.IsFalse((await walletStorage.SaveProviderWalletsForAvatarByIdAsync(walletAvatar,new(){[Core.Enums.ProviderType.SuiOASIS]=new(){
                    new Core.Objects.ProviderWallet{Id=Guid.NewGuid(),AvatarId=walletAvatar,ProviderType=Core.Enums.ProviderType.SuiOASIS,IsDefaultWallet=true,
                        WalletAddress=ownerIdentity.Result.WalletAddressLegacy,PublicKey=ownerIdentity.Result.PublicKey,PrivateKey=encrypted}}})).IsError);
                var dna=new API.DNA.OASISDNA();dna.OASIS.HyperDriveMode=API.DNA.HyperDriveModes.V2;dna.OASIS.StorageProviders=new(){LogSwitchingProviders=false};
                dna.OASIS.Security=new(){OASISProviderPrivateKeys=new(){Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('k',32)}};
                var providers=new Core.Managers.ProviderManager(null,dna);providers.RegisterProvider(walletStorage);providers.RegisterProvider(owner);
                Assert.IsFalse(providers.SetAndActivateCurrentStorageProvider(walletStorage).IsError);
                var ctor=typeof(Core.Managers.WalletManager).GetConstructors(System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Single(c=>c.GetParameters().Length==4);
                walletManager=(Core.Managers.WalletManager)ctor.Invoke(new object?[]{walletStorage,dna,providers,null});owner.WalletManager=walletManager;
                Record(new{walletAvatar,walletFile,depositId,escrowObjectId=settings.GetProperty("escrowObjectId").GetString()});
            }
            async Task<OASISResult<T>> Owned<T>(SuiOASIS provider,string signer,string stage,Func<Task<OASISResult<T>>> action) where T:ITransactionResponse {
                Record(new {stage,state="before-operation"});
                var result=await provider.ExecutePreparedNativeOperationAsync(action,execution,stage,signer,deadline.Token);
                Assert.IsFalse(result.IsError,result.Message);Assert.IsNotNull(result.Result);
                Record(new {stage,state="acknowledged",transactionId=result.Result.TransactionResult});return result;
            }
            var ownerAddress=ownerIdentity.Result.WalletAddressLegacy;var recipientAddress=recipientIdentity.Result.WalletAddressLegacy;
            async Task<string> NftScenario(){
            var minted=await Owned(owner,ownerAddress,"mint",()=>owner.MintNFTAsync(new MintWeb3NFTRequest {
                Title="Admitted CSharp oak 🌳",Description="Retained local native facade fixture",NumberToMint=nftCount,
                NFTStandardType=Core.Enums.NFTStandardType.SuiObject,StoreNFTMetaDataOnChain=!offChain,
                JSONMetaDataURL=offChain?"ipfs://bafy-test-owned-metadata/manifest.json":null,
                MetaData=new Dictionary<string,string>{{"park","owned native facade"}},Price=12.5m,RoyaltyPercentage=7,
                SendToAddressAfterMinting=ownerAddress}));
            var objects=minted.Result.Web3NFTs.ToList();Assert.AreEqual(nftCount,objects.Count);
            Assert.AreEqual(nftCount,objects.Select(x=>x.Id).Distinct().Count());
            Assert.AreEqual(nftCount,objects.Select(x=>x.NFTTokenAddress).Distinct().Count());
            Assert.AreEqual(objects[0].Id,minted.Result.Web3NFT.Id);
            Assert.AreEqual("Admitted CSharp oak 🌳",minted.Result.Web3NFT.Title,"Mint response must retain original caller metadata.");
            for(var index=0;index<objects.Count;index++){
            var nft=objects[index];Assert.IsFalse(string.IsNullOrWhiteSpace(nft.NFTTokenAddress));
            string Stage(string value)=>nftCount==1?value:value+"-"+index;
            Record(new {tokenId=nft.NFTTokenAddress,metadataId=nft.Id});
            var read=await owner.LoadOnChainNFTDataAsync(nft.NFTTokenAddress);Assert.IsFalse(read.IsError,read.Message);
            if(offChain){
                Assert.IsFalse(read.Result.StoreNFTMetaDataOnChain);
                Assert.AreEqual("ipfs://bafy-test-owned-metadata/manifest.json",read.Result.JSONMetaDataURL);
                Assert.IsTrue(string.IsNullOrEmpty(read.Result.Title));Assert.AreEqual(0m,read.Result.Price);
                Assert.IsFalse(read.Result.MetaData?.ContainsKey("park")==true);
                var invoke=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                var raw=await (Task<JsonElement>)invoke.Invoke(owner,new object[]{"nftGet",new {tokenId=nft.NFTTokenAddress},deadline.Token})!;
                var metadata=raw.GetProperty("metadata").GetString()!;
                Assert.IsFalse(metadata.Contains("owned native facade",StringComparison.Ordinal));
                Assert.IsFalse(metadata.Contains("Admitted CSharp oak",StringComparison.Ordinal));
                Record(new {stage="off-chain-projection-verified",tokenId=nft.NFTTokenAddress});
            }else{
                Assert.AreEqual("Admitted CSharp oak 🌳",read.Result.Title);Assert.AreEqual("owned native facade",read.Result.MetaData["park"]);
                Assert.AreEqual(12.5m,read.Result.Price);Assert.AreEqual(700u,read.Result.SellerFeeBasisPoints);
            }
            async Task RejectNonOwner(SuiOASIS signer,string address,string expectedOwner,string stage){
                var before=(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count;
                Record(new {stage,state="before-authorization-check"});
                var deniedSend=await signer.ExecutePreparedNativeOperationAsync(()=>signer.SendNFTAsync(new SendWeb3NFTRequest {
                    TokenId=nft.NFTTokenAddress,FromWalletAddress=address,ToWalletAddress=expectedOwner,Amount=1}),execution,stage+"-send",address,deadline.Token);
                Assert.IsTrue(deniedSend.IsError,"Non-owner transfer must fail.");
                Assert.AreEqual(before,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count,"Rejected transfer must not persist a signed attempt.");
                var deniedBurn=await signer.ExecutePreparedNativeOperationAsync(()=>signer.BurnNFTAsync(new BurnWeb3NFTRequest {
                    NFTTokenAddress=nft.NFTTokenAddress,OwnerPrivateKey="",OwnerPublicKey="",OwnerSeedPhrase=""}),execution,stage+"-burn",address,deadline.Token);
                Assert.IsTrue(deniedBurn.IsError,"Non-owner burn must fail.");
                Assert.AreEqual(before,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count,"Rejected burn must not persist a signed attempt.");
                var unchanged=await owner.LoadOnChainNFTDataAsync(nft.NFTTokenAddress);Assert.IsFalse(unchanged.IsError,unchanged.Message);
                Assert.AreEqual(expectedOwner,unchanged.Result.SendToAddressAfterMinting);
                Record(new {stage,state="rejected-without-signed-attempt",expectedOwner});
            }
            await RejectNonOwner(recipient,recipientAddress,ownerAddress,"recipient-before-transfer");
            await Owned(owner,ownerAddress,Stage("lock"),()=>owner.LockNFTAsync(new LockWeb3NFTRequest {NFTTokenAddress=nft.NFTTokenAddress,Web3NFTId=nft.Id}));
            await Owned(owner,ownerAddress,Stage("unlock"),()=>owner.UnlockNFTAsync(new UnlockWeb3NFTRequest {NFTTokenAddress=nft.NFTTokenAddress,Web3NFTId=nft.Id}));
            var sent=await Owned(owner,ownerAddress,Stage("send"),()=>owner.SendNFTAsync(new SendWeb3NFTRequest {TokenId=nft.NFTTokenAddress,FromWalletAddress=ownerAddress,ToWalletAddress=recipientAddress,Amount=1}));
            Assert.AreEqual(recipientAddress,sent.Result.Web3NFT.SendToAddressAfterMinting);
            await RejectNonOwner(owner,ownerAddress,recipientAddress,"former-owner-after-transfer");
            await Owned(recipient,recipientAddress,Stage("burn"),()=>recipient.BurnNFTAsync(new BurnWeb3NFTRequest {NFTTokenAddress=nft.NFTTokenAddress,
                OwnerPrivateKey=recipientKey,OwnerPublicKey=recipientIdentity.Result.PublicKey,OwnerSeedPhrase=""}));
            Assert.IsTrue((await owner.LoadOnChainNFTDataAsync(nft.NFTTokenAddress)).IsError);
            }
            return string.Join(",",objects.Select(x=>x.NFTTokenAddress));
            }
            async Task<string> TokenScenario(){
                var coinType=settings.GetProperty("coinType").GetString()!;
                async Task<decimal> Balance(SuiOASIS provider,string address,bool locked=false){
                    var balance=await provider.GetTokenBalanceAsync(address,coinType,locked);Assert.IsFalse(balance.IsError,balance.Message);return balance.Result;
                }
                async Task<string> Supply(){var probe=await (Task<JsonElement>)sdk.Invoke(owner,new object[]{"tokenProbe",null!,deadline.Token})!;return probe.GetProperty("treasury").GetProperty("supply").GetString()!;}
                foreach(var pair in new[]{(owner,ownerAddress),(recipient,recipientAddress)}){
                    Assert.AreEqual(0m,await Balance(pair.Item1,pair.Item2),"Retained balance requires reconciliation, not automatic burn.");
                    Assert.AreEqual(0m,await Balance(pair.Item1,pair.Item2,true),"Retained escrow requires reconciliation, not automatic unlock.");
                }
                var initialSupply=await Supply();
                if(idBound){
                    var escrowObjectId=settings.GetProperty("escrowObjectId").GetString()!;
                    Assert.AreEqual(JsonValueKind.Null,(await ReadSdk("tokenEscrowGet",new{depositId,escrowObjectId})).ValueKind);
                    await Owned(owner,ownerAddress,"mint",()=>owner.MintTokenAsync(new MintWeb3TokenRequest{MintedByAvatarId=walletAvatar,Amount=0.000001m,
                        MetaData=new(){{"TokenAddress",coinType},{"MintToWalletAddress",ownerAddress}}}));
                    Assert.AreEqual(0.000001m,await Balance(owner,ownerAddress));
                    var locking=new LockWeb4TokenRequest{LockedByAvatarId=walletAvatar,LockId=depositGuid,Amount=0.000000123m,TokenAddress=coinType,
                        EscrowContractAddress=escrowObjectId,FromWalletAddress=ownerAddress,ProviderType=new(Core.Enums.ProviderType.SuiOASIS)};
                    var locked=await Owned(owner,ownerAddress,"lock",()=>Task.FromResult(walletManager!.LockToken(locking)));
                    Assert.AreEqual(depositGuid,((SuiIdBoundEscrowResponse)locked.Result).LockId);Assert.AreEqual(0.000000877m,await Balance(owner,ownerAddress));
                    Assert.AreEqual(0.000000123m,await Balance(owner,ownerAddress,true));Assert.AreEqual(0m,await Balance(owner,recipientAddress,true));
                    var active=await ReadSdk("tokenEscrowGet",new{depositId,escrowObjectId});Assert.AreEqual("123",active.GetProperty("funds").GetString());
                    var unlocking=new UnlockWeb4TokenRequest{UnlockedByAvatarId=walletAvatar,LockId=depositGuid,TokenAddress=coinType,
                        EscrowContractAddress=escrowObjectId,ProviderType=new(Core.Enums.ProviderType.SuiOASIS)};
                    await Owned(owner,ownerAddress,"unlock",()=>walletManager!.UnlockTokenAsync(unlocking));
                    Assert.AreEqual(0.000001m,await Balance(owner,ownerAddress));
                    Assert.AreEqual(0m,await Balance(owner,ownerAddress,true));
                    var released=await ReadSdk("tokenEscrowGet",new{depositId,escrowObjectId});Assert.IsTrue(released.GetProperty("released").GetBoolean());Assert.AreEqual("0",released.GetProperty("funds").GetString());
                    await Owned(owner,ownerAddress,"burn",()=>owner.BurnTokenAsync(new BurnWeb3TokenRequest{BurntByAvatarId=walletAvatar,TokenAddress=coinType,
                        Amount=0.000001m,OwnerPrivateKey="",OwnerPublicKey="",OwnerSeedPhrase=""}));
                    Assert.AreEqual(0m,await Balance(owner,ownerAddress));Assert.AreEqual(initialSupply,await Supply());
                    Assert.AreEqual(encrypted,(await walletStorage!.LoadProviderWalletsForAvatarByIdAsync(walletAvatar)).Result[Core.Enums.ProviderType.SuiOASIS][0].PrivateKey);
                    Assert.IsFalse(owner.IsProviderActivated);return coinType;
                }
                var invalid=await owner.ExecutePreparedNativeOperationAsync(()=>owner.SendTokenAsync(new SendWeb3TokenRequest {
                    FromTokenAddress=coinType,FromWalletAddress=ownerAddress,ToWalletAddress=recipientAddress,Amount=0.0000000001m}),execution,"invalid-precision",ownerAddress,deadline.Token);
                Assert.IsTrue(invalid.IsError);Assert.AreEqual(0,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count);
                await Owned(owner,ownerAddress,"mint",()=>owner.MintTokenAsync(new MintWeb3TokenRequest {Amount=4m,MetaData=new() {{"TokenAddress",coinType},{"MintToWalletAddress",ownerAddress}}}));
                Assert.AreEqual(4m,await Balance(owner,ownerAddress));
                await Owned(owner,ownerAddress,"send",()=>owner.SendTokenAsync(new SendWeb3TokenRequest {FromTokenAddress=coinType,FromWalletAddress=ownerAddress,ToWalletAddress=recipientAddress,Amount=1m}));
                Assert.AreEqual(3m,await Balance(owner,ownerAddress));Assert.AreEqual(1m,await Balance(recipient,recipientAddress));
                await Owned(recipient,recipientAddress,"lock",()=>recipient.LockTokenAsync(new LockWeb3TokenRequest {TokenAddress=coinType,FromWalletAddress=recipientAddress}));
                Assert.AreEqual(0m,await Balance(recipient,recipientAddress));Assert.AreEqual(1m,await Balance(recipient,recipientAddress,true));
                await Owned(recipient,recipientAddress,"unlock",()=>recipient.UnlockTokenAsync(new UnlockWeb3TokenRequest {TokenAddress=coinType}));
                Assert.AreEqual(1m,await Balance(recipient,recipientAddress));Assert.AreEqual(0m,await Balance(recipient,recipientAddress,true));
                Task<OASISResult<ITransactionResponse>> Burn(SuiOASIS provider,decimal amount)=>provider.BurnTokenAsync(new BurnWeb3TokenRequest {
                    TokenAddress=coinType,Amount=amount,OwnerPrivateKey="",OwnerPublicKey="",OwnerSeedPhrase=""});
                await Owned(recipient,recipientAddress,"burn-partial",()=>Burn(recipient,0.25m));Assert.AreEqual(0.75m,await Balance(recipient,recipientAddress));
                await Owned(recipient,recipientAddress,"burn-holder",()=>Burn(recipient,0.75m));
                await Owned(owner,ownerAddress,"burn-issuer",()=>Burn(owner,3m));
                Assert.AreEqual(0m,await Balance(recipient,recipientAddress));Assert.AreEqual(0m,await Balance(owner,ownerAddress));
                Assert.AreEqual(initialSupply,await Supply());return coinType;
            }
            var assetId=token?await TokenScenario():await NftScenario();
            var stages=idBound?new[]{"mint","lock","unlock","burn"}:token?new[]{"mint","send","lock","unlock","burn-partial","burn-holder","burn-issuer"}:
                new[]{"mint"}.Concat(Enumerable.Range(0,nftCount).SelectMany(index=>new[]{"lock","unlock","send","burn"}.Select(stage=>nftCount==1?stage:stage+"-"+index))).ToArray();
            var retained=await store.GetAsync(execution.OperationId,deadline.Token);Assert.AreEqual(stages.Length,retained.ProviderAttempts.Count);
            CollectionAssert.AreEqual(stages.Select(x=>x+".storage.1").ToArray(),retained.ProviderAttempts.Select(x=>x.ClientRequestId).ToArray());
            var receipts=new List<object>();
            foreach(var attempt in retained.ProviderAttempts){
                var fee=await owner.GetPreparedNativeFeeReceiptAsync(attempt,deadline.Token);Assert.IsFalse(fee.IsError,fee.Message);
                Assert.IsTrue(fee.Result.GetProperty("success").GetBoolean());Assert.AreEqual(attempt.ProviderRequestId,fee.Result.GetProperty("transactionHash").GetString());
                Assert.AreEqual(attempt.NativePreparation.Sender,fee.Result.GetProperty("payer").GetString());
                receipts.Add(new {attempt.ClientRequestId,attempt.ProviderRequestId,payer=attempt.NativePreparation.Sender,
                    chainId=attempt.NativePreparation.ChainId,netUnits=fee.Result.GetProperty("netUnits").GetString(),success=true});
            }
            using(var report=new FileStream(reportPath,FileMode.Create,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(report,new {operationId=execution.OperationId,assetId,assetKind,scenario,
                    receipts,burned=true,productionBillingReconciled=false});report.Flush(true);
            }
            using(var report=JsonDocument.Parse(File.ReadAllBytes(reportPath)))Assert.AreEqual(stages.Length,report.RootElement.GetProperty("receipts").GetArrayLength());
            Console.WriteLine($"Native CSharp {assetKind} facade passed with{stages.Length} original signed attempts and bound native receipts; no external billing claim.");
            if(idBound){
                Assert.IsFalse((await walletStorage!.SaveProviderWalletsForAvatarByIdAsync(walletAvatar,new())).IsError);
                Assert.AreEqual("{}",(await File.ReadAllTextAsync(walletFile!)).Trim());File.Delete(walletFile!);
            }
            await mongo.DropDatabaseAsync(database);journal.Dispose();File.Delete(journalPath);
        } catch {Console.WriteLine("Retained exact facade journal/outbox for original-outcome reconciliation; no automatic mutation cleanup.");throw;}
        finally {if(walletStorage!=null)await walletStorage.DeActivateProviderAsync();walletLease?.Dispose();}
    }
}
