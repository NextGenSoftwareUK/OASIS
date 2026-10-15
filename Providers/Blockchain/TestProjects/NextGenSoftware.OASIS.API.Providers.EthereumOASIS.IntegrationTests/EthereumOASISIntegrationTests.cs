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
using NextGenSoftware.OASIS.API.Core.Objects.Search;
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

    [DataTestMethod]
    [DataRow("LegacyStorageProtocolFixture")]
    [DataRow("UnsupportedStorageProtocolFixture")]
    public async Task ActivationRejectsIncompatibleStorageProtocolWithoutSubmittingWrites(string fixtureName)
    {
        var path = Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON")!;
        using var fixture = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(path)!,fixtureName+".json")));
        var account = new Account(PrivateKey,ChainId);
        var web3 = new Web3(account,Host);
        var deployed = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
            fixture.RootElement.GetProperty("abi").GetRawText(),fixture.RootElement.GetProperty("bytecode").GetString(),
            account.Address,new Nethereum.Hex.HexTypes.HexBigInteger(4000000));
        Assert.AreEqual(BigInteger.One,deployed.Status.Value);
        var before = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address);
        var provider = new EthereumOASIS(Host,PrivateKey,ChainId,deployed.ContractAddress);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsTrue(activation.IsError); Assert.IsFalse(activation.Result);
        Assert.IsFalse(provider.IsProviderActivated); Assert.IsNull(provider.Web3Client);
        Assert.IsNotNull(activation.Exception);
        if(fixtureName == "UnsupportedStorageProtocolFixture") StringAssert.Contains(activation.Message,"version 2 is required");
        var after = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address);
        Assert.AreEqual(before.Value,after.Value);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeAvatarDetailCollectionReadsEncryptedRowsAndRejectsCorruption(bool synchronous)
    {
        var (provider,address) = await CreateActivatedProviderAsync();
        var empty = synchronous ? provider.LoadAllAvatarDetails() : await provider.LoadAllAvatarDetailsAsync();
        Assert.IsFalse(empty.IsError,empty.Message); Assert.IsTrue(empty.IsLoaded); Assert.AreEqual(0,empty.Result.Count());
        var first = new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id=Guid.NewGuid(),Username="native first",Karma=12,XP=34 };
        var second = new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id=Guid.NewGuid(),Username="native second",Karma=56,XP=78 };
        Assert.IsFalse((await provider.SaveAvatarDetailAsync(first)).IsError);
        Assert.IsFalse((await provider.SaveAvatarDetailAsync(second)).IsError);
        var loaded = synchronous ? provider.LoadAllAvatarDetails() : await provider.LoadAllAvatarDetailsAsync();
        Assert.IsFalse(loaded.IsError,loaded.Message); Assert.IsTrue(loaded.IsLoaded);
        Assert.AreEqual(2,loaded.Result.Count());
        Assert.AreEqual(first.Karma,loaded.Result.Single(row=>row.Id == first.Id).Karma);
        Assert.AreEqual(second.XP,loaded.Result.Single(row=>row.Id == second.Id).XP);
        var history = synchronous ? provider.LoadAllAvatarDetails(1) : await provider.LoadAllAvatarDetailsAsync(1);
        Assert.IsTrue(history.IsError); Assert.IsNull(history.Result); Assert.IsInstanceOfType(history.Exception,typeof(NotSupportedException));
        var service = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        await service.CreateAvatarDetailRequestAndWaitForReceiptAsync(
            NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(second.Id.ToString()),second.Id.ToString(),"plaintext corruption");
        var corrupt = synchronous ? provider.LoadAllAvatarDetails() : await provider.LoadAllAvatarDetailsAsync();
        Assert.IsTrue(corrupt.IsError); Assert.IsFalse(corrupt.IsLoaded); Assert.IsNull(corrupt.Result);
        Assert.IsNotNull(corrupt.Exception);
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeAvatarCollectionReturnsStoredRowsAndRejectsCorruption(bool synchronous)
    {
        var (provider,address) = await CreateActivatedProviderAsync();
        var empty = synchronous ? provider.LoadAllAvatars() : await provider.LoadAllAvatarsAsync();
        Assert.IsFalse(empty.IsError,empty.Message); Assert.IsTrue(empty.IsLoaded); Assert.AreEqual(0,empty.Result.Count());
        var first = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=Guid.NewGuid(),Username="first avatar",Email="first@example.invalid" };
        var second = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=Guid.NewGuid(),Username="second avatar",Email="second@example.invalid" };
        Assert.IsFalse((await provider.SaveAvatarAsync(first)).IsError);
        Assert.IsFalse((await provider.SaveAvatarAsync(second)).IsError);
        var loaded = synchronous ? provider.LoadAllAvatars() : await provider.LoadAllAvatarsAsync();
        Assert.IsFalse(loaded.IsError,loaded.Message); Assert.IsTrue(loaded.IsLoaded); Assert.AreEqual(2,loaded.Result.Count());
        Assert.AreEqual(first.Email,loaded.Result.Single(row=>row.Id == first.Id).Email);
        Assert.AreEqual(second.Username,loaded.Result.Single(row=>row.Id == second.Id).Username);
        var history = synchronous ? provider.LoadAllAvatars(1) : await provider.LoadAllAvatarsAsync(1);
        Assert.IsTrue(history.IsError); Assert.IsNull(history.Result); Assert.IsInstanceOfType(history.Exception,typeof(NotSupportedException));
        var service = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        await service.CreateAvatarRequestAndWaitForReceiptAsync(
            NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(second.Id.ToString()),second.Id.ToString(),"plaintext corruption");
        var corrupt = synchronous ? provider.LoadAllAvatars() : await provider.LoadAllAvatarsAsync();
        Assert.IsTrue(corrupt.IsError); Assert.IsFalse(corrupt.IsLoaded); Assert.IsNull(corrupt.Result); Assert.IsNotNull(corrupt.Exception);
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(true,false)]
    [DataRow(false,true)]
    [DataRow(true,true)]
    public async Task CollectionRejectsAuthenticCiphertextUnderCollidingPublicIdentity(bool synchronous,bool details)
    {
        var (provider,address) = await CreateActivatedProviderAsync();
        var original = Guid.Parse("00005f42-0000-0000-0000-000000000000");
        var other = Guid.Parse("00017b6d-0000-0000-0000-000000000000");
        var key = NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(original.ToString());
        Assert.AreEqual(key,NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(other.ToString()));
        var service = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        if(details)
        {
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id=original,Username="authentic detail" })).IsError);
            var encrypted = (await service.GetAvatarDetailByIdQueryAsync(key)).ReturnValue1.Info;
            await service.DeleteAvatarDetailRequestAndWaitForReceiptAsync(key);
            await service.CreateAvatarDetailRequestAndWaitForReceiptAsync(key,other.ToString(),encrypted);
            var result = synchronous ? provider.LoadAllAvatarDetails() : await provider.LoadAllAvatarDetailsAsync();
            Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsLoaded); Assert.IsNull(result.Result);
            Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
            StringAssert.Contains(result.Exception!.Message,"requested GUID");
        }
        else
        {
            Assert.IsFalse((await provider.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=original,Username="authentic avatar" })).IsError);
            var encrypted = (await service.GetAvatarByIdQueryAsync(key)).ReturnValue1.Info;
            await service.DeleteAvatarRequestAndWaitForReceiptAsync(key);
            await service.CreateAvatarRequestAndWaitForReceiptAsync(key,other.ToString(),encrypted);
            var result = synchronous ? provider.LoadAllAvatars() : await provider.LoadAllAvatarsAsync();
            Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsLoaded); Assert.IsNull(result.Result);
            Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
            StringAssert.Contains(result.Exception!.Message,"requested GUID");
        }
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeHolonCollectionHonorsTypesAndGraphReadOptions(bool synchronous)
    {
        var (provider,address) = await CreateActivatedProviderAsync();
        async Task<OASISResult<IEnumerable<IHolon>>> Read(bool load=true,bool recursive=true,int max=0,int current=0,bool native=false,HolonType type=HolonType.All,int version=0)
            => synchronous ? provider.LoadAllHolons(type,load,recursive,max,current,loadChildrenFromProvider:native,version:version)
                : await provider.LoadAllHolonsAsync(type,load,recursive,max,current,loadChildrenFromProvider:native,version:version);
        var empty = await Read(); Assert.IsFalse(empty.IsError,empty.Message); Assert.AreEqual(0,empty.Result.Count());
        var root = new CoreHolon { Id=Guid.NewGuid(),Name="root",HolonType=HolonType.Holon };
        var child = new CoreHolon { Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="child",HolonType=HolonType.Holon };
        var grandchild = new CoreHolon { Id=Guid.NewGuid(),ParentHolonId=child.Id,Name="grandchild",HolonType=HolonType.Holon };
        child.Children.Add(grandchild); root.Children.Add(child);
        foreach(var row in new[] {root,child,grandchild}) Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
        foreach(var native in new[] {false,true})
        {
            var all = await Read(native:native); Assert.IsFalse(all.IsError,all.Message); Assert.IsTrue(all.IsLoaded); Assert.AreEqual(3,all.Result.Count());
            Assert.AreEqual(grandchild.Id,all.Result.Single(row=>row.Id==root.Id).Children.Single().Children.Single().Id);
            var flat = await Read(load:false,native:native); Assert.IsFalse(flat.IsError,flat.Message); Assert.IsTrue(flat.Result.All(row=>row.Children.Count==0));
            var direct = await Read(recursive:false,native:native); Assert.IsFalse(direct.IsError,direct.Message);
            Assert.AreEqual(0,direct.Result.Single(row=>row.Id==root.Id).Children.Single().Children.Count);
            var depth = await Read(max:1,native:native); Assert.IsFalse(depth.IsError,depth.Message);
            Assert.AreEqual(0,depth.Result.Single(row=>row.Id==root.Id).Children.Single().Children.Count);
            var offset = await Read(max:1,current:1,native:native); Assert.IsFalse(offset.IsError,offset.Message); Assert.IsTrue(offset.Result.All(row=>row.Children.Count==0));
        }
        var filtered = await Read(type:HolonType.Avatar); Assert.IsFalse(filtered.IsError,filtered.Message); Assert.AreEqual(0,filtered.Result.Count());
        var history = await Read(version:1); Assert.IsTrue(history.IsError); Assert.IsNull(history.Result);
        var invalid = await Read(max:-1); Assert.IsTrue(invalid.IsError); Assert.IsNull(invalid.Result);
        // A provider-linked cycle must be rejected, not silently truncated.
        root.ParentHolonId=grandchild.Id;
        Assert.IsFalse((await provider.SaveHolonAsync(root,saveChildren:false)).IsError);
        var cycle = await Read(native:true); Assert.IsTrue(cycle.IsError); Assert.IsNull(cycle.Result); Assert.IsFalse(cycle.IsLoaded);
        var service = new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        await service.CreateHolonRequestAndWaitForReceiptAsync(NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(child.Id.ToString()),child.Id.ToString(),"corrupt payload");
        var corrupt = await Read(load:false); Assert.IsTrue(corrupt.IsError); Assert.IsNull(corrupt.Result); Assert.IsFalse(corrupt.IsLoaded);
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(true,false)]
    [DataRow(false,true)]
    [DataRow(true,true)]
    public async Task NativeIdentityQueriesUseStoredFieldsAndRejectAmbiguity(bool synchronous,bool details)
    {
        var (provider,address) = await CreateActivatedProviderAsync();
        var id=Guid.NewGuid();
        const string username="RealStoredName",email="Stored@Example.invalid";
        if(details) Assert.IsFalse((await provider.SaveAvatarDetailAsync(new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id=id,Username=username,Email=email,XP=77 })).IsError);
        else Assert.IsFalse((await provider.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=id,Username=username,Email=email })).IsError);
        async Task<(bool Error,Guid? Id,bool Loaded,string Code,Exception? Cause)> Query(string value,bool byEmail=false,int version=0)
        {
            if(details)
            {
                var result = byEmail
                    ? synchronous ? provider.LoadAvatarDetailByEmail(value,version) : await provider.LoadAvatarDetailByEmailAsync(value,version)
                    : synchronous ? provider.LoadAvatarDetailByUsername(value,version) : await provider.LoadAvatarDetailByUsernameAsync(value,version);
                return (result.IsError,result.Result?.Id,result.IsLoaded,result.ErrorCode,result.Exception);
            }
            else
            {
                var result = byEmail
                    ? synchronous ? provider.LoadAvatarByEmail(value,version) : await provider.LoadAvatarByEmailAsync(value,version)
                    : synchronous ? provider.LoadAvatarByUsername(value,version) : await provider.LoadAvatarByUsernameAsync(value,version);
                return (result.IsError,result.Result?.Id,result.IsLoaded,result.ErrorCode,result.Exception);
            }
        }
        foreach(var byEmail in new[] {false,true})
        {
            var found=await Query((byEmail?email:username).ToUpperInvariant(),byEmail);
            Assert.IsFalse(found.Error); Assert.IsTrue(found.Loaded); Assert.AreEqual(id,found.Id);
            var absent=await Query("not-present",byEmail); Assert.IsTrue(absent.Error); Assert.IsNull(absent.Id); Assert.IsFalse(absent.Loaded);
            var blank=await Query(" ",byEmail); Assert.IsTrue(blank.Error); Assert.IsInstanceOfType(blank.Cause,typeof(ArgumentException));
            var history=await Query(byEmail?email:username,byEmail,1); Assert.IsTrue(history.Error); Assert.IsInstanceOfType(history.Cause,typeof(NotSupportedException));
        }
        if(details)
        {
            Assert.AreEqual(id,(await provider.LoadAvatarDetailByEmailVersionAsync(email)).Result.Id);
            Assert.AreEqual(id,(await provider.LoadAvatarDetailByUsernameVersionAsync(username)).Result.Id);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id=Guid.NewGuid(),Username=username,Email=email })).IsError);
        }
        else Assert.IsFalse((await provider.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=Guid.NewGuid(),Username=username,Email=email })).IsError);
        foreach(var byEmail in new[] {false,true})
        {
            var ambiguous=await Query(byEmail?email:username,byEmail);
            Assert.IsTrue(ambiguous.Error); Assert.IsNull(ambiguous.Id); Assert.IsFalse(ambiguous.Loaded);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,ambiguous.Code);
            Assert.IsInstanceOfType(ambiguous.Cause,typeof(NextGenSoftware.OASIS.Common.StorageIdentityAmbiguousException));
        }
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(true,false)]
    [DataRow(false,true)]
    [DataRow(true,true)]
    public async Task V2AmbiguousNativeIdentityCannotFallThroughToUniqueBackup(bool synchronous,bool details)
    {
        var (ethereum,_) = await CreateActivatedProviderAsync();
        var folder=Path.GetDirectoryName(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON"))!;
        var path=Path.Combine(folder,"v2-identity.db");
        if(File.Exists(path)) throw new InvalidOperationException("Existing identity fixture requires inspection; refusing overwrite.");
        using var sqlite=new SQLiteProvider($"Data Source={path};Pooling=False");
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            const string username="v2-duplicate-identity",email="v2-duplicate@example.invalid";
            for(var index=0;index<2;index++)
            {
                if(details) Assert.IsFalse((await ethereum.SaveAvatarDetailAsync(new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id=Guid.NewGuid(),Username=username,Email=email })).IsError);
                else Assert.IsFalse((await ethereum.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=Guid.NewGuid(),Username=username,Email=email })).IsError);
            }
            var backupId=Guid.NewGuid();
            if(details) Assert.IsFalse((await sqlite.SaveAvatarDetailAsync(new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id=backupId,Username=username,Email=email })).IsError);
            else
            {
                var saved=await sqlite.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=backupId,Username=username,Email=email,AvatarType=new EnumValue<AvatarType>(AvatarType.User) });
                Assert.IsFalse(saved.IsError,saved.Message);
            }
            var dna=new OASISDNA { OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna) { IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true };
            manager.RegisterProvider(ethereum); manager.RegisterProvider(sqlite);
            var stores=new[] {new EnumValue<ProviderType>(ProviderType.EthereumOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(stores).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(stores).IsError);
            var router=new OASISHyperDrive(manager);
            foreach(var byEmail in new[] {false,true})
            {
                var request=new StorageOperationRequest { Operation=details ? byEmail?"LoadAvatarDetailByEmail":"LoadAvatarDetailByUsername" : byEmail?"LoadAvatarByEmail":"LoadAvatarByUsername",
                    Username=username,Email=email,PreferredProvider=ProviderType.EthereumOASIS };
                if(details)
                {
                    var result=synchronous ? router.RouteRequest<IAvatarDetail>(request) : await router.RouteRequestAsync<IAvatarDetail>(request);
                    Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
                    Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);
                }
                else
                {
                    var result=synchronous ? router.RouteRequest<IAvatar>(request) : await router.RouteRequestAsync<IAvatar>(request);
                    Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
                    Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);
                }
            }
            Assert.AreEqual(2L,manager.PerformanceMonitor.GetMetrics(ProviderType.EthereumOASIS).TotalRequests);
            Assert.AreEqual(0L,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0L);
            if(details) Assert.AreEqual(backupId,(await sqlite.LoadAvatarDetailByUsernameAsync(username)).Result.Id);
            else Assert.AreEqual(backupId,(await sqlite.LoadAvatarByUsernameAsync(username)).Result.Id);
        }
        finally { sqlite.Dispose(); File.Delete(path); await ethereum.DeActivateProviderAsync(); }
    }

    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(true,false)]
    [DataRow(false,true)]
    [DataRow(true,true)]
    public async Task MissingGuidCannotReportLoadedSuccess(bool synchronous,bool populated)
    {
        var (provider,_) = await CreateActivatedProviderAsync();
        if(populated)
        {
            Assert.IsFalse((await provider.SaveHolonAsync(new CoreHolon { Id=Guid.NewGuid(),Name="retained" })).IsError);
            Assert.IsFalse((await provider.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=Guid.NewGuid(),Username="retained" })).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id=Guid.NewGuid(),Username="retained" })).IsError);
        }
        var absent=Guid.NewGuid();
        var holon=synchronous ? provider.LoadHolon(absent) : await provider.LoadHolonAsync(absent);
        var avatar=synchronous ? provider.LoadAvatar(absent) : await provider.LoadAvatarAsync(absent);
        var detail=synchronous ? provider.LoadAvatarDetail(absent) : await provider.LoadAvatarDetailAsync(absent);
        Assert.IsTrue(holon.IsError); Assert.IsFalse(holon.IsLoaded); Assert.IsNull(holon.Result); Assert.IsNotNull(holon.Exception);
        Assert.IsTrue(avatar.IsError); Assert.IsFalse(avatar.IsLoaded); Assert.IsNull(avatar.Result); Assert.IsNotNull(avatar.Exception);
        Assert.IsTrue(detail.IsError); Assert.IsFalse(detail.IsLoaded); Assert.IsNull(detail.Result); Assert.IsNotNull(detail.Exception);
        if(populated)
        {
            Assert.IsInstanceOfType(holon.Exception,typeof(KeyNotFoundException));
            Assert.IsInstanceOfType(avatar.Exception,typeof(KeyNotFoundException));
            Assert.IsInstanceOfType(detail.Exception,typeof(KeyNotFoundException));
        }
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ProviderKeysResolveSavedNativeIdentitiesWithoutCollisionOrSyntheticData(bool synchronous)
    {
        var (provider,_) = await CreateActivatedProviderAsync();
        var id=Guid.Parse("00005f42-0000-0000-0000-000000000000");
        var collision=Guid.Parse("00017b6d-0000-0000-0000-000000000000");
        var savedHolon=await provider.SaveHolonAsync(new CoreHolon { Id=id,Name="keyed holon" });
        var savedAvatar=await provider.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=id,Username="keyed avatar" });
        Assert.IsFalse(savedHolon.IsError,savedHolon.Message); Assert.IsFalse(savedAvatar.IsError,savedAvatar.Message);
        Assert.AreEqual(id.ToString(),savedHolon.Result.ProviderUniqueStorageKey[ProviderType.EthereumOASIS]);
        Assert.AreEqual(id.ToString(),savedAvatar.Result.ProviderUniqueStorageKey[ProviderType.EthereumOASIS]);
        var key=id.ToString().ToUpperInvariant();
        var holon=synchronous ? provider.LoadHolon(key) : await provider.LoadHolonAsync(key);
        var avatar=synchronous ? provider.LoadAvatarByProviderKey(key) : await provider.LoadAvatarByProviderKeyAsync(key);
        Assert.IsFalse(holon.IsError,holon.Message); Assert.IsTrue(holon.IsLoaded); Assert.AreEqual(id,holon.Result.Id); Assert.AreEqual("keyed holon",holon.Result.Name);
        Assert.IsFalse(avatar.IsError,avatar.Message); Assert.IsTrue(avatar.IsLoaded); Assert.AreEqual(id,avatar.Result.Id); Assert.AreEqual("keyed avatar",avatar.Result.Username);
        Assert.AreEqual(id.ToString(),avatar.Result.ProviderUniqueStorageKey[ProviderType.EthereumOASIS]);
        Assert.AreEqual(id,(await provider.LoadAvatarByProviderKeyVersionAsync(key)).Result.Id);
        Assert.AreEqual(1,(await provider.LoadAllAvatarsByVersionAsync()).Result.Count());
        foreach(var bad in new[] {"not-a-guid",Guid.Empty.ToString(),collision.ToString(),Guid.NewGuid().ToString()})
        {
            var h=synchronous ? provider.LoadHolon(bad) : await provider.LoadHolonAsync(bad);
            var a=synchronous ? provider.LoadAvatarByProviderKey(bad) : await provider.LoadAvatarByProviderKeyAsync(bad);
            Assert.IsTrue(h.IsError); Assert.IsNull(h.Result); Assert.IsFalse(h.IsLoaded);
            Assert.IsTrue(a.IsError); Assert.IsNull(a.Result); Assert.IsFalse(a.IsLoaded);
            if(bad==collision.ToString()) { Assert.IsInstanceOfType(h.Exception,typeof(InvalidDataException)); Assert.IsInstanceOfType(a.Exception,typeof(InvalidDataException)); }
        }
        Assert.IsTrue((await provider.LoadHolonAsync(key,version:1)).IsError);
        Assert.IsTrue((await provider.LoadAvatarByProviderKeyVersionAsync(key,1)).IsError);
        Assert.IsTrue((await provider.LoadAllAvatarsByVersionAsync(1)).IsError);
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ParentQueriesUseNativeSnapshotAndPreserveReadErrors(bool synchronous)
    {
        var (provider,address) = await CreateActivatedProviderAsync();
        var parent=Guid.NewGuid();
        var child=new CoreHolon { Id=Guid.NewGuid(),ParentHolonId=parent,Name="direct child",HolonType=HolonType.Holon };
        var grandchild=new CoreHolon { Id=Guid.NewGuid(),ParentHolonId=child.Id,Name="grandchild",HolonType=HolonType.Holon };
        var unrelated=new CoreHolon { Id=Guid.NewGuid(),ParentHolonId=Guid.NewGuid(),Name="unrelated",HolonType=HolonType.Holon };
        child.Children.Add(grandchild);
        foreach(var row in new[] {child,grandchild,unrelated}) Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
        async Task<OASISResult<IEnumerable<IHolon>>> Read(bool byKey=false,bool children=true,HolonType type=HolonType.All,int version=0)
        {
            if(byKey) return synchronous ? provider.LoadHolonsForParent(parent.ToString().ToUpperInvariant(),type,children,loadChildrenFromProvider:true,version:version)
                : await provider.LoadHolonsForParentAsync(parent.ToString().ToUpperInvariant(),type,children,loadChildrenFromProvider:true,version:version);
            return synchronous ? provider.LoadHolonsForParent(parent,type,children,loadChildrenFromProvider:true,version:version)
                : await provider.LoadHolonsForParentAsync(parent,type,children,loadChildrenFromProvider:true,version:version);
        }
        foreach(var byKey in new[] {false,true})
        {
            var loaded=await Read(byKey); Assert.IsFalse(loaded.IsError,loaded.Message); Assert.IsTrue(loaded.IsLoaded);
            Assert.AreEqual(child.Id,loaded.Result.Single().Id); Assert.AreEqual(grandchild.Id,loaded.Result.Single().Children.Single().Id);
            var flat=await Read(byKey,children:false); Assert.IsFalse(flat.IsError,flat.Message); Assert.AreEqual(0,flat.Result.Single().Children.Count);
            var filtered=await Read(byKey,type:HolonType.Avatar); Assert.IsFalse(filtered.IsError,filtered.Message); Assert.AreEqual(0,filtered.Result.Count());
            var history=await Read(byKey,version:1); Assert.IsTrue(history.IsError); Assert.IsNull(history.Result); Assert.IsInstanceOfType(history.Exception,typeof(NotSupportedException));
        }
        var malformed=await provider.LoadHolonsForParentAsync("not-a-guid"); Assert.IsTrue(malformed.IsError); Assert.IsInstanceOfType(malformed.Exception,typeof(ArgumentException));
        var absent=await provider.LoadHolonsForParentAsync(Guid.NewGuid()); Assert.IsFalse(absent.IsError,absent.Message); Assert.AreEqual(0,absent.Result.Count());
        var service=new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        await service.CreateHolonRequestAndWaitForReceiptAsync(NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(child.Id.ToString()),child.Id.ToString(),"corrupt payload");
        var corrupt=await Read(); Assert.IsTrue(corrupt.IsError); Assert.IsNull(corrupt.Result); Assert.IsFalse(corrupt.IsLoaded); Assert.IsNotNull(corrupt.Exception);
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SingleNativeHolonReadHonorsGraphFlagsAndRejectsFakeHistory(bool synchronous)
    {
        var (provider,_) = await CreateActivatedProviderAsync();
        var root=new CoreHolon { Id=Guid.NewGuid(),Name="single root" };
        var child=new CoreHolon { Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="single child" };
        var grandchild=new CoreHolon { Id=Guid.NewGuid(),ParentHolonId=child.Id,Name="single grandchild" };
        child.Children.Add(grandchild); root.Children.Add(child);
        foreach(var row in new[] {root,child,grandchild}) Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
        async Task<OASISResult<IHolon>> Read(bool load=true,bool recursive=true,int max=0,bool native=false,int version=0)
            => synchronous ? provider.LoadHolon(root.Id,load,recursive,max,loadChildrenFromProvider:native,version:version)
                : await provider.LoadHolonAsync(root.Id,load,recursive,max,loadChildrenFromProvider:native,version:version);
        foreach(var native in new[] {false,true})
        {
            var full=await Read(native:native); Assert.IsFalse(full.IsError,full.Message); Assert.IsTrue(full.IsLoaded);
            Assert.AreEqual(grandchild.Id,full.Result.Children.Single().Children.Single().Id);
            var flat=await Read(load:false,native:native); Assert.IsFalse(flat.IsError,flat.Message); Assert.AreEqual(0,flat.Result.Children.Count);
            var direct=await Read(recursive:false,native:native); Assert.IsFalse(direct.IsError,direct.Message); Assert.AreEqual(0,direct.Result.Children.Single().Children.Count);
            var depth=await Read(max:1,native:native); Assert.IsFalse(depth.IsError,depth.Message); Assert.AreEqual(0,depth.Result.Children.Single().Children.Count);
        }
        var versioned=await Read(version:1); Assert.IsTrue(versioned.IsError); Assert.IsNull(versioned.Result); Assert.IsInstanceOfType(versioned.Exception,typeof(NotSupportedException));
        var invalid=await Read(max:-1); Assert.IsTrue(invalid.IsError); Assert.IsNull(invalid.Result); Assert.IsInstanceOfType(invalid.Exception,typeof(ArgumentOutOfRangeException));
        // Provider-linked children must be loaded even when absent in the root's embedded payload.
        root.Children.Clear(); Assert.IsFalse((await provider.SaveHolonAsync(root,saveChildren:false)).IsError);
        Assert.AreEqual(0,(await Read()).Result.Children.Count);
        Assert.AreEqual(grandchild.Id,(await Read(native:true)).Result.Children.Single().Children.Single().Id);
        var key=synchronous ? provider.LoadHolon(root.Id.ToString(),loadChildrenFromProvider:true)
            : await provider.LoadHolonAsync(root.Id.ToString(),loadChildrenFromProvider:true);
        Assert.IsFalse(key.IsError,key.Message); Assert.AreEqual(child.Id,key.Result.Children.Single().Id);
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MetadataQueriesReadEncryptedNativeFieldsWithAllAndAnySemantics(bool synchronous)
    {
        var (provider,address)=await CreateActivatedProviderAsync();
        var first=new CoreHolon { Id=Guid.NewGuid(),Name="first metadata",MetaData=new Dictionary<string,object> { ["Region"]="park",["Count"]=5,["Enabled"]=true } };
        var second=new CoreHolon { Id=Guid.NewGuid(),Name="second metadata",MetaData=new Dictionary<string,object> { ["Region"]="park",["Count"]=9 } };
        foreach(var row in new[] {first,second}) Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
        async Task<OASISResult<IEnumerable<IHolon>>> Read(Dictionary<string,string> query,MetaKeyValuePairMatchMode mode=MetaKeyValuePairMatchMode.All,int version=0)
            => synchronous ? provider.LoadHolonsByMetaData(query,mode,loadChildren:false,version:version)
                : await provider.LoadHolonsByMetaDataAsync(query,mode,loadChildren:false,version:version);
        var single=synchronous ? provider.LoadHolonsByMetaData("Region","park",loadChildren:false) : await provider.LoadHolonsByMetaDataAsync("Region","park",loadChildren:false);
        Assert.IsFalse(single.IsError,single.Message); Assert.IsTrue(single.IsLoaded); Assert.AreEqual(2,single.Result.Count());
        var query=new Dictionary<string,string> { ["Region"]="park",["Count"]="5" };
        var all=await Read(query); Assert.IsFalse(all.IsError,all.Message); Assert.AreEqual(first.Id,all.Result.Single().Id);
        var any=await Read(query,MetaKeyValuePairMatchMode.Any); Assert.IsFalse(any.IsError,any.Message); Assert.AreEqual(2,any.Result.Count());
        var boolean=await Read(new Dictionary<string,string> { ["Enabled"]="True" }); Assert.IsFalse(boolean.IsError,boolean.Message); Assert.AreEqual(first.Id,boolean.Result.Single().Id);
        var absent=await Read(new Dictionary<string,string> { ["region"]="park" }); Assert.IsFalse(absent.IsError,absent.Message); Assert.AreEqual(0,absent.Result.Count());
        var history=await Read(query,version:1); Assert.IsTrue(history.IsError); Assert.IsInstanceOfType(history.Exception,typeof(NotSupportedException));
        var empty=await Read(new Dictionary<string,string>()); Assert.IsTrue(empty.IsError); Assert.IsInstanceOfType(empty.Exception,typeof(ArgumentException));
        var invalid=await Read(query,(MetaKeyValuePairMatchMode)99); Assert.IsTrue(invalid.IsError); Assert.IsInstanceOfType(invalid.Exception,typeof(ArgumentOutOfRangeException));
        var service=new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        await service.CreateHolonRequestAndWaitForReceiptAsync(NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(second.Id.ToString()),second.Id.ToString(),"corrupt payload");
        var corrupt=await Read(query); Assert.IsTrue(corrupt.IsError); Assert.IsNull(corrupt.Result); Assert.IsFalse(corrupt.IsLoaded); Assert.IsNotNull(corrupt.Exception);
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CustomKeyQueriesHaveOneStoredMeaningAndRejectDuplicateParents(bool synchronous)
    {
        var (provider,_) = await CreateActivatedProviderAsync();
        var unrelatedId=Guid.NewGuid();
        var customKey=unrelatedId.ToString();
        var parent=new CoreHolon { Id=Guid.NewGuid(),Name="custom parent",MetaData=new Dictionary<string,object> { ["CustomKey"]=customKey } };
        var child=new CoreHolon { Id=Guid.NewGuid(),ParentHolonId=parent.Id,Name="custom child" };
        var unrelated=new CoreHolon { Id=unrelatedId,Name="must not win provider-key fallback" };
        foreach(var row in new[] {parent,child,unrelated}) Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
        var loaded=synchronous ? provider.LoadHolonByCustomKey(customKey) : await provider.LoadHolonByCustomKeyAsync(customKey);
        Assert.IsFalse(loaded.IsError,loaded.Message); Assert.IsTrue(loaded.IsLoaded); Assert.AreEqual(parent.Id,loaded.Result.Id);
        var children=synchronous ? provider.LoadHolonsForParentByCustomKey(customKey,loadChildren:false)
            : await provider.LoadHolonsForParentByCustomKeyAsync(customKey,loadChildren:false);
        Assert.IsFalse(children.IsError,children.Message); Assert.IsTrue(children.IsLoaded); Assert.AreEqual(child.Id,children.Result.Single().Id);
        var missing=await provider.LoadHolonByCustomKeyAsync("absent-custom-key"); Assert.IsTrue(missing.IsError); Assert.IsInstanceOfType(missing.Exception,typeof(KeyNotFoundException));
        var blank=await provider.LoadHolonsForParentByCustomKeyAsync(" "); Assert.IsTrue(blank.IsError); Assert.IsInstanceOfType(blank.Exception,typeof(ArgumentException));
        var history=await provider.LoadHolonByCustomKeyAsync(customKey,version:1); Assert.IsTrue(history.IsError); Assert.IsInstanceOfType(history.Exception,typeof(NotSupportedException));
        Assert.IsFalse((await provider.SaveHolonAsync(new CoreHolon { Id=Guid.NewGuid(),MetaData=new Dictionary<string,object> { ["CustomKey"]=customKey } },saveChildren:false)).IsError);
        var duplicate=synchronous ? provider.LoadHolonByCustomKey(customKey) : await provider.LoadHolonByCustomKeyAsync(customKey);
        Assert.IsTrue(duplicate.IsError); Assert.IsNull(duplicate.Result); Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,duplicate.ErrorCode);
        var duplicateParent=synchronous ? provider.LoadHolonsForParentByCustomKey(customKey) : await provider.LoadHolonsForParentByCustomKeyAsync(customKey);
        Assert.IsTrue(duplicateParent.IsError); Assert.IsNull(duplicateParent.Result); Assert.IsFalse(duplicateParent.IsLoaded);
        Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,duplicateParent.ErrorCode);
        Assert.IsInstanceOfType(duplicateParent.Exception,typeof(StorageIdentityAmbiguousException));
        await provider.DeActivateProviderAsync();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeSearchFiltersStoredRecordsAndPreservesReadFailures(bool synchronous)
    {
        var (provider,address)=await CreateActivatedProviderAsync();
        var owner=Guid.NewGuid();
        var root=new CoreHolon { Id=Guid.NewGuid(),Name="forest guardian",Version=7,CreatedByAvatarId=owner,CreatedDate=new DateTime(2026,1,1),MetaData=new Dictionary<string,object> { ["Region"]="park" } };
        var child=new CoreHolon { Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="forest seed",Version=2,CreatedDate=new DateTime(2020,1,1) };
        foreach(var row in new[] {root,child}) Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
        Assert.IsFalse((await provider.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id=Guid.NewGuid(),Username="forest friend",CreatedDate=new DateTime(2020,1,1) })).IsError);
        SearchParams Query(params ISearchGroupBase[] groups)=>new SearchParams { SearchOnlyForCurrentAvatar=false,SearchGroups=groups.ToList() };
        async Task<OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.Search.ISearchResults>> Run(SearchParams query,int version=0)
            => synchronous ? provider.Search(query,loadChildren:false,version:version) : await provider.SearchAsync(query,loadChildren:false,version:version);
        var text=new SearchTextGroup { SearchQuery="guardian",SearchHolons=true,HolonType=HolonType.All };
        var found=await Run(Query(text)); Assert.IsFalse(found.IsError,found.Message); Assert.IsTrue(found.IsLoaded);
        Assert.AreEqual(root.Id,found.Result.SearchResultHolons.Single().Id); Assert.AreEqual(0,found.Result.SearchResultAvatars.Count);
        var both=await Run(Query(new SearchTextGroup { SearchQuery="forest",HolonType=HolonType.All }));
        Assert.IsFalse(both.IsError,both.Message); Assert.AreEqual(3,both.Result.NumberOfResults);
        var number=new SearchNumberGroup { Number=5,NumberOperator=SearchOperatorType.GreaterThan,SearchHolons=true,HolonType=HolonType.All,PreviousSearchGroupOperator=SearchParamGroupOperator.And };
        var combined=await Run(Query(new SearchTextGroup { SearchQuery="forest",SearchHolons=true,HolonType=HolonType.All },number));
        Assert.IsFalse(combined.IsError,combined.Message); Assert.AreEqual(root.Id,combined.Result.SearchResultHolons.Single().Id);
        var date=await Run(Query(new SearchDateGroup { Date=new DateTime(2025,1,1),DateOperator=SearchOperatorType.GreaterThan,SearchHolons=true,HolonType=HolonType.All }));
        Assert.IsFalse(date.IsError,date.Message); Assert.AreEqual(root.Id,date.Result.SearchResultHolons.Single().Id);
        var owned=Query(); owned.SearchOnlyForCurrentAvatar=true; owned.AvatarId=owner;
        Assert.AreEqual(root.Id,(await Run(owned)).Result.SearchResultHolons.Single().Id);
        var parent=Query(); parent.ParentId=root.Id; Assert.AreEqual(child.Id,(await Run(parent)).Result.SearchResultHolons.Single().Id);
        var metadata=Query(); metadata.FilterByMetaData=new Dictionary<string,string> { ["Region"]="park" };
        Assert.AreEqual(root.Id,(await Run(metadata)).Result.SearchResultHolons.Single().Id);
        var history=await Run(Query(text),1); Assert.IsTrue(history.IsError); Assert.IsInstanceOfType(history.Exception,typeof(NotSupportedException));
        var invalid=await Run(Query(new SearchTextGroup { SearchQuery=" " })); Assert.IsTrue(invalid.IsError); Assert.IsNull(invalid.Result);
        var service=new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        await service.CreateHolonRequestAndWaitForReceiptAsync(NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(child.Id.ToString()),child.Id.ToString(),"corrupt payload");
        var corrupt=await Run(Query(text)); Assert.IsTrue(corrupt.IsError); Assert.IsNull(corrupt.Result); Assert.IsFalse(corrupt.IsLoaded); Assert.IsNotNull(corrupt.Exception);
        await provider.DeActivateProviderAsync();
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
