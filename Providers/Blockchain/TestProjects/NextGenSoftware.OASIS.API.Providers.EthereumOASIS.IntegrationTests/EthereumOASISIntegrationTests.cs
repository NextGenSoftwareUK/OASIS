using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition;
using CoreHolon = NextGenSoftware.OASIS.API.Core.Holons.Holon;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using SQLiteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public class EthereumOASISIntegrationTests
{
    private const string DefaultHost = "http://127.0.0.1:8545";
    // Ganache's documented deterministic development key. It has no value outside the disposable chain.
    private const string DefaultPrivateKey = "0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d";
    private static readonly BigInteger DefaultChainId = 31337;

    private static string Host => Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_RPC") ?? DefaultHost;
    private static string PrivateKey => Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY") ?? DefaultPrivateKey;
    private static BigInteger ChainId => BigInteger.Parse(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_CHAIN_ID") ?? DefaultChainId.ToString());

    private static async Task<(EthereumOASIS Provider, string Contract)> CreateActivatedProviderAsync()
    {
        var account = new Account(PrivateKey, ChainId);
        var web3 = new Web3(account, Host);
        var receipt = await NextGenSoftwareOASISService.DeployContractAndWaitForReceiptAsync(web3, new NextGenSoftwareOASISDeployment());
        Assert.IsFalse(receipt.HasErrors(), "The disposable EVM storage contract deployment failed.");

        var provider = new EthereumOASIS(Host, PrivateKey, ChainId, receipt.ContractAddress);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        Assert.IsTrue(activation.Result);
        Assert.IsTrue(provider.IsProviderActivated);
        return (provider, receipt.ContractAddress);
    }

    [TestMethod]
    public async Task Activation_ValidatesRpcChainAndDeployedContract()
    {
        var (provider, contract) = await CreateActivatedProviderAsync();
        Assert.IsFalse(string.IsNullOrWhiteSpace(contract));
        Assert.AreEqual(ProviderType.EthereumOASIS, provider.ProviderType.Value);

        var deactivation = await provider.DeActivateProviderAsync();
        Assert.IsFalse(deactivation.IsError, deactivation.Message);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task Holon_SaveLoadRoundTrip_UsesDisposableEvmChain()
    {
        var (provider, _) = await CreateActivatedProviderAsync();
        var holon = new CoreHolon
        {
            Id = Guid.NewGuid(),
            Name = $"ethereum-integration-{Guid.NewGuid():N}",
            Description = "Disposable Ganache round-trip"
        };

        var saved = await provider.SaveHolonAsync(holon);
        Assert.IsFalse(saved.IsError, saved.Message);
        Assert.IsTrue(saved.IsSaved);

        var nativeService = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),provider.ContractAddress);
        var payload = (await nativeService.GetHolonByIdQueryAsync(
            NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(holon.Id.ToString()))).ReturnValue1.Info;
        StringAssert.StartsWith(payload,"oasis-aes256gcm-gzip-v2:");
        Assert.IsFalse(payload.Contains(holon.Name));

        var loaded = await provider.LoadHolonAsync(holon.Id);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.IsNotNull(loaded.Result);
        Assert.AreEqual(holon.Id, loaded.Result.Id);
        Assert.AreEqual(holon.Name, loaded.Result.Name);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GuidHashCollisionCannotReturnAnotherPrivateRecord(bool synchronous)
    {
        var storedId = Guid.Parse("00005f42-0000-0000-0000-000000000000");
        var otherId = Guid.Parse("00017b6d-0000-0000-0000-000000000000");
        Assert.AreNotEqual(storedId,otherId);
        Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(storedId.ToString()),
            NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(otherId.ToString()));
        var (provider, _) = await CreateActivatedProviderAsync();
        Assert.IsFalse((await provider.SaveHolonAsync(new CoreHolon { Id=storedId,Name="collision fixture" })).IsError);
        Assert.IsFalse((await provider.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=storedId,Username="collision fixture" })).IsError);
        Assert.IsFalse((await provider.SaveAvatarDetailAsync(new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id=storedId,Username="collision fixture" })).IsError);
        var holon = synchronous ? provider.LoadHolon(otherId) : await provider.LoadHolonAsync(otherId);
        var avatar = synchronous ? provider.LoadAvatar(otherId) : await provider.LoadAvatarAsync(otherId);
        var detail = synchronous ? provider.LoadAvatarDetail(otherId) : await provider.LoadAvatarDetailAsync(otherId);
        Assert.IsTrue(holon.IsError); Assert.IsNull(holon.Result); Assert.IsFalse(holon.IsLoaded);
        Assert.IsTrue(avatar.IsError); Assert.IsNull(avatar.Result); Assert.IsFalse(avatar.IsLoaded);
        Assert.IsTrue(detail.IsError); Assert.IsNull(detail.Result); Assert.IsFalse(detail.IsLoaded);
        Assert.IsInstanceOfType(holon.Exception,typeof(InvalidDataException));
        Assert.IsInstanceOfType(avatar.Exception,typeof(InvalidDataException));
        Assert.IsInstanceOfType(detail.Exception,typeof(InvalidDataException));
        var deletedHolon = synchronous ? provider.DeleteHolon(otherId) : await provider.DeleteHolonAsync(otherId);
        var deletedAvatar = synchronous ? provider.DeleteAvatar(otherId,softDelete:false) : await provider.DeleteAvatarAsync(otherId,softDelete:false);
        Assert.IsTrue(deletedHolon.IsError); Assert.IsFalse(deletedHolon.IsDeleted); Assert.AreEqual(0,deletedHolon.DeletedCount);
        Assert.IsTrue(deletedAvatar.IsError); Assert.IsFalse(deletedAvatar.Result);
        StringAssert.Contains(deletedHolon.Exception.ToString(),"OASIS identity collision");
        StringAssert.Contains(deletedAvatar.Exception.ToString(),"OASIS identity collision");
        Assert.AreEqual(storedId,(await provider.LoadHolonAsync(storedId)).Result.Id);
        Assert.AreEqual(storedId,(await provider.LoadAvatarAsync(storedId)).Result.Id);
        Assert.AreEqual(storedId,(await provider.LoadAvatarDetailAsync(storedId)).Result.Id);
        var correctHolon = synchronous ? provider.DeleteHolon(storedId) : await provider.DeleteHolonAsync(storedId);
        var correctAvatar = synchronous ? provider.DeleteAvatar(storedId,softDelete:false) : await provider.DeleteAvatarAsync(storedId,softDelete:false);
        Assert.IsFalse(correctHolon.IsError,correctHolon.Message); Assert.IsTrue(correctHolon.IsDeleted);
        Assert.IsFalse(correctAvatar.IsError,correctAvatar.Message); Assert.IsTrue(correctAvatar.Result);
        var service = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),provider.ContractAddress);
        Assert.AreEqual(BigInteger.Zero,await service.GetHolonsCountQueryAsync());
        Assert.AreEqual(BigInteger.Zero,await service.GetAvatarsCountQueryAsync());
        Assert.AreEqual(BigInteger.One,await service.GetAvatarDetailsCountQueryAsync());
        var handler = new Web3(new Account(PrivateKey,ChainId),Host).Eth.GetContractHandler(provider.ContractAddress);
        Exception? detailCollision = null;
        try { await handler.SendRequestAndWaitForReceiptAsync(new DeleteAvatarDetailByIdentityFunction {
            EntityId=NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(otherId.ToString()),Identity=otherId.ToString() }); }
        catch(Exception error) { detailCollision=error; }
        Assert.IsNotNull(detailCollision);
        StringAssert.Contains(detailCollision.ToString(),"OASIS identity collision");
        Assert.AreEqual(storedId,(await provider.LoadAvatarDetailAsync(storedId)).Result.Id);
        var detailReceipt = await handler.SendRequestAndWaitForReceiptAsync(new DeleteAvatarDetailByIdentityFunction {
            EntityId=NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(storedId.ToString()),Identity=storedId.ToString() });
        Assert.AreEqual(BigInteger.One,detailReceipt.Status.Value);
        Assert.AreEqual(BigInteger.Zero,await service.GetAvatarDetailsCountQueryAsync());
    }

    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(true,false)]
    [DataRow(false,true)]
    [DataRow(true,true)]
    public async Task BulkNativeRejectionRetainsCommittedEvidence(bool synchronous, bool throughV2)
    {
        var fixturePath = Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON")
            ?? throw new InvalidOperationException("Compile the retained local rejection fixture first.");
        using var fixture = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(fixturePath));
        var account = new Account(PrivateKey,ChainId);
        var web3 = new Web3(account,Host);
        var deployed = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
            fixture.RootElement.GetProperty("abi").GetRawText(),fixture.RootElement.GetProperty("bytecode").GetString(),
            account.Address,new Nethereum.Hex.HexTypes.HexBigInteger(4000000));
        Assert.AreEqual(BigInteger.One,deployed.Status.Value);
        var provider = new EthereumOASIS(Host,PrivateKey,ChainId,deployed.ContractAddress);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var first = new CoreHolon { Id = Guid.NewGuid(), Name = "owned committed first holon" };
        var second = new CoreHolon { Id = Guid.NewGuid(), Name = "owned rejected second holon" };
        var batch = new[] { first,second };
        SQLiteProvider? backup = null;
        var backupPath = Path.Combine(Path.GetDirectoryName(fixturePath)!,"v2-backup.db");
        try
        {
        OASISResult<IEnumerable<IHolon>> result;
        ProviderManager? manager = null;
        if (throughV2)
        {
            if (File.Exists(backupPath)) throw new InvalidOperationException("Existing backup fixture requires inspection; refusing overwrite.");
            backup = new SQLiteProvider($"Data Source={backupPath};Pooling=False");
            Assert.IsFalse((await backup.ActivateProviderAsync()).IsError);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            manager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = true };
            manager.RegisterProvider(provider);
            manager.RegisterProvider(backup);
            var stores = new[] { new EnumValue<ProviderType>(ProviderType.EthereumOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(stores).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(stores).IsError);
            var router = new OASISHyperDrive(manager);
            var request = new StorageOperationRequest { Operation = "SaveHolons", Payload = batch,
                PreferredProvider = ProviderType.EthereumOASIS, SaveChildren = false };
            result = synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request) : await router.RouteRequestAsync<IEnumerable<IHolon>>(request);
        }
        else result = synchronous ? provider.SaveHolons(batch,saveChildren:false) : await provider.SaveHolonsAsync(batch,saveChildren:false);
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.IsSaved);
        Assert.IsInstanceOfType(result.Exception,typeof(EthereumPartialStorageMutationException));
        var evidence = (EthereumPartialStorageMutationException)result.Exception;
        Assert.AreEqual(1,evidence.CommittedTransactions.Count);
        Assert.IsNotNull(evidence.InnerException);
        var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(evidence.OriginalTransactionId);
        Assert.AreEqual(BigInteger.One,receipt.Status.Value);
        var firstRead = await provider.LoadHolonAsync(first.Id);
        Assert.IsFalse(firstRead.IsError,firstRead.Message);
        Assert.AreEqual(first.Name,firstRead.Result.Name);
        var rejectedRead = await provider.LoadHolonAsync(second.Id);
        Assert.IsNull(rejectedRead.Result);
        if (throughV2)
        {
            Assert.AreEqual(0L,manager!.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            Assert.AreEqual(1L,manager.PerformanceMonitor.GetMetrics(ProviderType.EthereumOASIS).TotalRequests);
            foreach (var node in batch) Assert.IsNull((await backup!.LoadHolonAsync(node.Id,loadChildren:false)).Result);
        }
        }
        finally
        {
            if (backup != null)
            {
                backup.Dispose();
                File.Delete(backupPath);
            }
        }
        await provider.DeActivateProviderAsync();
    }

    [TestMethod]
    public async Task NativeHolonUpdateDoesNotDependOnAvatarDetailsCount()
    {
        var (provider,contract) = await CreateActivatedProviderAsync();
        var web3 = new Web3(new Account(PrivateKey,ChainId),Host);
        var service = new NextGenSoftwareOASISService(web3,contract);
        Assert.AreEqual(BigInteger.Zero,await service.GetAvatarDetailsCountQueryAsync());
        var identity = new BigInteger(101);
        var created = await service.CreateHolonRequestAndWaitForReceiptAsync(identity,Guid.NewGuid().ToString(),"before");
        Assert.AreEqual(BigInteger.One,created.Status.Value);
        var updated = await service.UpdateHolonRequestAndWaitForReceiptAsync(identity,"after");
        Assert.AreEqual(BigInteger.One,updated.Status.Value);
        Assert.AreEqual("after",(await service.GetHolonByIdQueryAsync(identity)).ReturnValue1.Info);
        await provider.DeActivateProviderAsync();
    }

    [TestMethod]
    public async Task Activation_RejectsWrongChainId()
    {
        var provider = new EthereumOASIS(Host, PrivateKey, ChainId + 1, "0x0000000000000000000000000000000000000001");

        var activation = await provider.ActivateProviderAsync();
        Assert.IsTrue(activation.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        StringAssert.Contains(activation.Message, "does not match configured chain ID");
    }

    [TestMethod]
    public async Task PrivateReadsRejectPlaintextAndCrossTypeCiphertext()
    {
        var (provider,address) = await CreateActivatedProviderAsync();
        var service = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        var holon = new CoreHolon { Id=Guid.NewGuid(),Name="synthetic encrypted source" };
        Assert.IsFalse((await provider.SaveHolonAsync(holon,saveChildren:false)).IsError);
        var entityId = NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(holon.Id.ToString());
        var ciphertext = (await service.GetHolonByIdQueryAsync(entityId)).ReturnValue1.Info;
        await service.CreateAvatarRequestAndWaitForReceiptAsync(entityId,holon.Id.ToString(),ciphertext);
        var crossed = await provider.LoadAvatarAsync(holon.Id);
        Assert.IsTrue(crossed.IsError);
        Assert.IsNull(crossed.Result);
        Assert.IsInstanceOfType(crossed.Exception,typeof(System.Security.Cryptography.CryptographicException));
        var plain = new CoreHolon { Id=Guid.NewGuid(),Name="synthetic plaintext must reject" };
        await service.CreateHolonRequestAndWaitForReceiptAsync(
            NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(plain.Id.ToString()),plain.Id.ToString(),Newtonsoft.Json.JsonConvert.SerializeObject(plain));
        var rejected = await provider.LoadHolonAsync(plain.Id,loadChildren:false);
        Assert.IsTrue(rejected.IsError);
        Assert.IsNull(rejected.Result);
        Assert.IsInstanceOfType(rejected.Exception,typeof(System.Security.Cryptography.CryptographicException));
        Assert.AreEqual(holon.Name,(await provider.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name);
        await provider.DeActivateProviderAsync();
    }

    [TestMethod]
    public async Task AvatarAndDetailPayloadsAreEncryptedWithRealNativeReadback()
    {
        var (provider,address) = await CreateActivatedProviderAsync();
        var service = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=Guid.NewGuid(),
            Username="synthetic-private-avatar",Email="private-avatar@example.invalid",Password="synthetic-password-hash" };
        var saved = await provider.SaveAvatarAsync(avatar);
        Assert.IsFalse(saved.IsError,saved.Message);
        var raw = (await service.GetAvatarByIdQueryAsync(
            NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(avatar.Id.ToString()))).ReturnValue1.Info;
        StringAssert.StartsWith(raw,"oasis-aes256gcm-gzip-v2:");
        Assert.IsFalse(raw.Contains(avatar.Email));
        Assert.IsFalse(raw.Contains(avatar.Password));
        var read = await provider.LoadAvatarAsync(avatar.Id);
        Assert.IsFalse(read.IsError,read.Message);
        Assert.AreEqual(avatar.Email,read.Result.Email);
        Assert.AreEqual(avatar.Password,read.Result.Password);
        var detail = new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id=Guid.NewGuid(),
            Email="private-detail@example.invalid",Karma=654,XP=321 };
        var savedDetail = await provider.SaveAvatarDetailAsync(detail);
        Assert.IsFalse(savedDetail.IsError,savedDetail.Message);
        var rawDetail = (await service.GetAvatarDetailByIdQueryAsync(
            NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(detail.Id.ToString()))).ReturnValue1.Info;
        StringAssert.StartsWith(rawDetail,"oasis-aes256gcm-gzip-v2:");
        Assert.IsFalse(rawDetail.Contains(detail.Email));
        var readDetail = await provider.LoadAvatarDetailAsync(detail.Id);
        Assert.IsFalse(readDetail.IsError,readDetail.Message);
        Assert.AreEqual(detail.Email,readDetail.Result.Email);
        Assert.AreEqual(detail.Karma,readDetail.Result.Karma);
        Assert.AreEqual(detail.XP,readDetail.Result.XP);
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MissingPrivateStorageKeyDoesNotSubmitAnEthereumTransaction(bool synchronous)
    {
        var (provider,address) = await CreateActivatedProviderAsync();
        var web3 = new Web3(new Account(PrivateKey,ChainId),Host);
        var service = new NextGenSoftwareOASISService(web3,address);
        var before = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(new Account(PrivateKey,ChainId).Address);
        var prior = Environment.GetEnvironmentVariable("OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY",null);
            var holon = new CoreHolon { Id=Guid.NewGuid(),Name="must not be published" };
            var result = synchronous ? provider.SaveHolon(holon,saveChildren:false) : await provider.SaveHolonAsync(holon,saveChildren:false);
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(result.IsSaved);
            Assert.IsNotNull(result.Exception);
        }
        finally { Environment.SetEnvironmentVariable("OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY",prior); }
        Assert.AreEqual(before.Value,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(new Account(PrivateKey,ChainId).Address)).Value);
        Assert.AreEqual(BigInteger.Zero,await service.GetHolonsCountQueryAsync());
        await provider.DeActivateProviderAsync();
    }

    [TestMethod]
    public async Task ActivationRejectsSignerThatDoesNotOwnStorageWithoutSubmission()
    {
        var (owner,contract) = await CreateActivatedProviderAsync();
        var unrelatedKey = Nethereum.Signer.EthECKey.GenerateKey().GetPrivateKey();
        var unrelated = new Account(unrelatedKey,ChainId);
        var reader = new Web3(Host);
        var nonceBefore = await reader.Eth.Transactions.GetTransactionCount.SendRequestAsync(unrelated.Address);
        var provider = new EthereumOASIS(Host,unrelatedKey,ChainId,contract);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsTrue(activation.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsNotNull(activation.Exception);
        StringAssert.Contains(activation.Message,"does not own the storage contract");
        var nonceAfter = await reader.Eth.Transactions.GetTransactionCount.SendRequestAsync(unrelated.Address);
        Assert.AreEqual(nonceBefore.Value,nonceAfter.Value);
        await owner.DeActivateProviderAsync();
    }

    [TestMethod]
    public async Task StorageMutationsRejectAnUnrelatedNativeAccount()
    {
        var (provider,address) = await CreateActivatedProviderAsync();
        var service = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        await service.CreateHolonRequestAndWaitForReceiptAsync(101,"holon","holon-original");
        await service.CreateAvatarRequestAndWaitForReceiptAsync(101,"avatar","avatar-original");
        await service.CreateAvatarDetailRequestAndWaitForReceiptAsync(101,"detail","detail-original");
        var fixturePath = Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON")!;
        using var compiled = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(fixturePath)!,"default-storage.json")));
        var unlocked = new Web3(Host);
        var accounts = await unlocked.Eth.Accounts.SendRequestAsync();
        var owner = new Account(PrivateKey,ChainId).Address;
        var outsider = accounts.First(account => !string.Equals(account,owner,StringComparison.OrdinalIgnoreCase));
        var contract = unlocked.Eth.GetContract(compiled.RootElement.GetProperty("abi").GetRawText(),address);
        foreach(var name in new[] {"CreateHolon","CreateAvatar","CreateAvatarDetail","UpdateHolon","UpdateAvatar","UpdateAvatarDetail","DeleteHolon","DeleteAvatar","DeleteAvatarDetail","DeleteHolonByIdentity","DeleteAvatarByIdentity","DeleteAvatarDetailByIdentity"})
        {
            object[] input = name.StartsWith("Create") ? new object[] {new BigInteger(999),"unrelated-id","unrelated-value"}
                : name.StartsWith("Update") || name.EndsWith("ByIdentity") ? new object[] {new BigInteger(101),"corrupted"} : new object[] {new BigInteger(101)};
            var denied = false;
            try
            {
                var receipt = await contract.GetFunction(name).SendTransactionAndWaitForReceiptAsync(outsider,
                    new Nethereum.Hex.HexTypes.HexBigInteger(2000000),null,null,input);
                denied = receipt.Status?.Value == 0;
            }
            catch(Exception error) { denied = error.ToString().Contains("OASIS storage owner required",StringComparison.Ordinal); }
            Assert.IsTrue(denied,"Unrelated account must not execute " + name);
        }
        Assert.AreEqual("holon-original",(await service.GetHolonByIdQueryAsync(101)).ReturnValue1.Info);
        Assert.AreEqual("avatar-original",(await service.GetAvatarByIdQueryAsync(101)).ReturnValue1.Info);
        Assert.AreEqual("detail-original",(await service.GetAvatarDetailByIdQueryAsync(101)).ReturnValue1.Info);
        await provider.DeActivateProviderAsync();
    }

    [TestMethod]
    public async Task NativeDeleteThenInsertKeepsAllRecordArraysQueryable()
    {
        var (provider,contract) = await CreateActivatedProviderAsync();
        var service = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),contract);
        async Task Check(Func<BigInteger,string,string,Task<Nethereum.RPC.Eth.DTOs.TransactionReceipt>> save,
            Func<BigInteger,Task<Nethereum.RPC.Eth.DTOs.TransactionReceipt>> remove,
            Func<BigInteger,Task<string>> read,Func<Task<BigInteger>> count)
        {
            await save(301,"first","deleted record");
            await save(302,"retained","retained record");
            Assert.AreEqual(BigInteger.One,(await remove(301)).Status.Value);
            Assert.AreEqual(BigInteger.One,await count());
            await save(303,"inserted","new record after deletion");
            Assert.AreEqual(new BigInteger(2),await count());
            Assert.AreEqual("retained record",await read(302));
            Assert.AreEqual("new record after deletion",await read(303));
            await remove(302);
            await remove(303);
            Assert.AreEqual(BigInteger.Zero,await count());
            await save(304,"after-empty","new record after empty array");
            Assert.AreEqual(BigInteger.One,await count());
            Assert.AreEqual("new record after empty array",await read(304));
        }
        await Check((id,key,info)=>service.CreateHolonRequestAndWaitForReceiptAsync(id,key,info),
            id=>service.DeleteHolonRequestAndWaitForReceiptAsync(id),async id=>(await service.GetHolonByIdQueryAsync(id)).ReturnValue1.Info,
            ()=>service.GetHolonsCountQueryAsync());
        await Check((id,key,info)=>service.CreateAvatarRequestAndWaitForReceiptAsync(id,key,info),
            id=>service.DeleteAvatarRequestAndWaitForReceiptAsync(id),async id=>(await service.GetAvatarByIdQueryAsync(id)).ReturnValue1.Info,
            ()=>service.GetAvatarsCountQueryAsync());
        await Check((id,key,info)=>service.CreateAvatarDetailRequestAndWaitForReceiptAsync(id,key,info),
            id=>service.DeleteAvatarDetailRequestAndWaitForReceiptAsync(id),async id=>(await service.GetAvatarDetailByIdQueryAsync(id)).ReturnValue1.Info,
            ()=>service.GetAvatarDetailsCountQueryAsync());
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MissingNativeMutationCannotProduceSuccessfulReceipt(bool populated)
    {
        var (provider,address) = await CreateActivatedProviderAsync();
        var service = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        if(populated)
        {
            await service.CreateHolonRequestAndWaitForReceiptAsync(901,"retained","holon");
            await service.CreateAvatarRequestAndWaitForReceiptAsync(901,"retained","avatar");
            await service.CreateAvatarDetailRequestAndWaitForReceiptAsync(901,"retained","detail");
        }
        foreach(var attempt in new Func<Task<Nethereum.RPC.Eth.DTOs.TransactionReceipt>>[] {
            ()=>service.UpdateHolonRequestAndWaitForReceiptAsync(902,"missing"),
            ()=>service.UpdateAvatarRequestAndWaitForReceiptAsync(902,"missing"),
            ()=>service.UpdateAvatarDetailRequestAndWaitForReceiptAsync(902,"missing"),
            ()=>service.DeleteHolonRequestAndWaitForReceiptAsync(902),
            ()=>service.DeleteAvatarRequestAndWaitForReceiptAsync(902),
            ()=>service.DeleteAvatarDetailRequestAndWaitForReceiptAsync(902)
        })
        {
            Exception? rejection = null;
            try { var receipt=await attempt(); Assert.AreEqual(BigInteger.Zero,receipt.Status.Value,"Missing mutation must not succeed"); }
            catch(Exception error) when(error is not AssertFailedException) { rejection=error; }
            if(rejection != null && populated) StringAssert.Contains(rejection.ToString(),"OASIS record not found");
        }
        var expected = populated ? BigInteger.One : BigInteger.Zero;
        Assert.AreEqual(expected,await service.GetHolonsCountQueryAsync());
        Assert.AreEqual(expected,await service.GetAvatarsCountQueryAsync());
        Assert.AreEqual(expected,await service.GetAvatarDetailsCountQueryAsync());
        if(populated)
        {
            Assert.AreEqual("holon",(await service.GetHolonByIdQueryAsync(901)).ReturnValue1.Info);
            Assert.AreEqual("avatar",(await service.GetAvatarByIdQueryAsync(901)).ReturnValue1.Info);
            Assert.AreEqual("detail",(await service.GetAvatarDetailByIdQueryAsync(901)).ReturnValue1.Info);
        }
        await provider.DeActivateProviderAsync();
    }

    [TestMethod]
    public async Task RepeatedStorageSavesUpdateOneIdentityRatherThanAppendDuplicates()
    {
        var (provider,contract) = await CreateActivatedProviderAsync();
        var service = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),contract);
        var holon = new CoreHolon { Id = Guid.NewGuid(), Name = "before repeated save" };
        Assert.IsFalse((await provider.SaveHolonAsync(holon,saveChildren:false)).IsError);
        holon.Name = "after repeated save";
        Assert.IsFalse((await provider.SaveHolonAsync(holon,saveChildren:false)).IsError);
        Assert.AreEqual(holon.Name,(await provider.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name);
        Assert.AreEqual(BigInteger.One,await service.GetHolonsCountQueryAsync());
        var avatarId = Guid.NewGuid().ToString();
        await service.CreateAvatarRequestAndWaitForReceiptAsync(201,avatarId,"avatar-before");
        await service.CreateAvatarRequestAndWaitForReceiptAsync(201,avatarId,"avatar-after");
        Assert.AreEqual("avatar-after",(await service.GetAvatarByIdQueryAsync(201)).ReturnValue1.Info);
        Assert.AreEqual(BigInteger.One,await service.GetAvatarsCountQueryAsync());
        await service.CreateAvatarDetailRequestAndWaitForReceiptAsync(202,avatarId,"detail-before");
        await service.CreateAvatarDetailRequestAndWaitForReceiptAsync(202,avatarId,"detail-after");
        Assert.AreEqual("detail-after",(await service.GetAvatarDetailByIdQueryAsync(202)).ReturnValue1.Info);
        Assert.AreEqual(BigInteger.One,await service.GetAvatarDetailsCountQueryAsync());
        foreach(var attempt in new Func<Task<Nethereum.RPC.Eth.DTOs.TransactionReceipt>>[] {
            () => service.CreateHolonRequestAndWaitForReceiptAsync(
                NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(holon.Id.ToString()),Guid.NewGuid().ToString(),"wrong identity"),
            () => service.CreateAvatarRequestAndWaitForReceiptAsync(201,Guid.NewGuid().ToString(),"wrong identity"),
            () => service.CreateAvatarDetailRequestAndWaitForReceiptAsync(202,Guid.NewGuid().ToString(),"wrong identity")
        })
        {
            Exception? collision = null;
            try { await attempt(); } catch(Exception error) { collision=error; }
            Assert.IsNotNull(collision,"a reused numeric key must not overwrite a different public identity");
            StringAssert.Contains(collision.ToString(),"OASIS identity collision");
        }
        Assert.AreEqual(holon.Name,(await provider.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name);
        Assert.AreEqual("avatar-after",(await service.GetAvatarByIdQueryAsync(201)).ReturnValue1.Info);
        Assert.AreEqual("detail-after",(await service.GetAvatarDetailByIdQueryAsync(202)).ReturnValue1.Info);
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task V2HealthyReplicationAndRoundRobinUseBothRealStores(bool synchronous)
    {
        var (ethereum,_) = await CreateActivatedProviderAsync();
        var fixturePath = Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON")
            ?? throw new InvalidOperationException("Owned reusable runner required.");
        var path = Path.Combine(Path.GetDirectoryName(fixturePath)!,"v2-priority.db");
        if(File.Exists(path)) throw new InvalidOperationException("Existing fixture requires inspection.");
        using var sqlite = new SQLiteProvider($"Data Source={path};Pooling=False");
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var stores = new[] { new EnumValue<ProviderType>(ProviderType.EthereumOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
            var replication = new ProviderManager(null,dna) { IsAutoReplicationEnabled = true };
            replication.RegisterProvider(ethereum);
            replication.RegisterProvider(sqlite);
            Assert.IsFalse(replication.SetAndReplaceAutoReplicationListForProviders(stores).IsError);
            var router = new OASISHyperDrive(replication);
            CoreHolon? selected = null;
            foreach(var primary in new[] { ProviderType.EthereumOASIS,ProviderType.SQLLiteDBOASIS })
            {
                var holon = new CoreHolon { Id = Guid.NewGuid(), Name = "owned v2 replication " + primary };
                selected = holon;
                var request = new StorageOperationRequest { Operation = "SaveHolon", Payload = holon, PreferredProvider = primary, SaveChildren = false };
                var saved = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
                Assert.IsFalse(saved.IsError,saved.Message);
                Assert.IsFalse(saved.IsWarning,saved.Message);
                foreach(var store in new IOASISStorageProvider[] { ethereum,sqlite })
                {
                    var loaded = await store.LoadHolonAsync(holon.Id,loadChildren:false);
                    Assert.IsFalse(loaded.IsError,loaded.Message);
                    Assert.AreEqual(holon.Name,loaded.Result.Name);
                    Assert.AreEqual(holon.Id,loaded.Result.Id);
                }
            }
            var balancing = new ProviderManager(null,dna) { IsAutoLoadBalanceEnabled = true };
            balancing.RegisterProvider(ethereum);
            balancing.RegisterProvider(sqlite);
            Assert.IsFalse(balancing.SetAndReplaceAutoLoadBalanceListForProviders(stores).IsError);
            var balanced = new OASISHyperDrive(balancing);
            for(var index=0;index<6;index++)
            {
                var read = new StorageOperationRequest { Operation = "LoadHolon", HolonId = selected!.Id, LoadChildren = false };
                var loaded = synchronous ? balanced.RouteRequest<IHolon>(read,LoadBalancingStrategy.RoundRobin)
                    : await balanced.RouteRequestAsync<IHolon>(read,LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(loaded.IsError,loaded.Message);
                Assert.AreEqual(selected.Id,loaded.Result.Id);
                Assert.AreEqual(selected.Name,loaded.Result.Name);
            }
            foreach(var store in new[] { ProviderType.EthereumOASIS,ProviderType.SQLLiteDBOASIS })
                Assert.AreEqual(3L,balancing.PerformanceMonitor.GetMetrics(store).TotalRequests);

            // No service is allowed on this owned fixture endpoint: failure occurs before signing/submission.
            var unavailable = new EthereumOASIS("http://127.0.0.1:18546",PrivateKey,ChainId,ethereum.ContractAddress);
            var failover = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true };
            failover.RegisterProvider(unavailable);
            failover.RegisterProvider(sqlite);
            Assert.IsFalse(failover.SetAndReplaceAutoFailOverListForProviders(stores).IsError);
            var failoverRouter = new OASISHyperDrive(failover);
            var recovered = new CoreHolon { Id = Guid.NewGuid(), Name = "owned pre-submission failover" };
            var recoveryRequest = new StorageOperationRequest { Operation = "SaveHolon", Payload = recovered,
                PreferredProvider = ProviderType.EthereumOASIS, SaveChildren = false };
            var recovery = synchronous ? failoverRouter.RouteRequest<IHolon>(recoveryRequest)
                : await failoverRouter.RouteRequestAsync<IHolon>(recoveryRequest);
            Assert.IsFalse(recovery.IsError,recovery.Message);
            Assert.AreEqual(recovered.Id,recovery.Result.Id);
            Assert.AreEqual(recovered.Name,(await sqlite.LoadHolonAsync(recovered.Id,loadChildren:false)).Result.Name);
            Assert.IsNull((await ethereum.LoadHolonAsync(recovered.Id,loadChildren:false)).Result);
            Assert.AreEqual(1L,failover.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS).TotalRequests);
        }
        finally
        {
            sqlite.Dispose();
            File.Delete(path);
            await ethereum.DeActivateProviderAsync();
        }
    }
}
