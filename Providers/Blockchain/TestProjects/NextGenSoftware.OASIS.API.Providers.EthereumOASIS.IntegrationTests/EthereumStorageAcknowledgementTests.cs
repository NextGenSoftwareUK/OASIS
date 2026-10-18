using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
using SQLiteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    private sealed class LostStorageReceiptProvider : EthereumOASIS
    {
        public readonly IOException Cause=new("Owned response loss after actual native receipt.");
        public string? SubmittedHash;
        public int Polls;
        public LostStorageReceiptProvider(string contract):base(Host,PrivateKey,EthereumOASISIntegrationTests.ChainId,contract) { }
        protected override async Task<TransactionReceipt> PollStorageReceiptAsync(string transactionId)
        {
            var receipt=await base.PollStorageReceiptAsync(transactionId);
            Assert.AreEqual(BigInteger.One,receipt.Status.Value);
            SubmittedHash=transactionId;Polls++;
            throw Cause;
        }
    }

    [DataTestMethod]
    [DataRow(false,"SaveHolon")]
    [DataRow(true,"SaveHolon")]
    [DataRow(false,"SaveAvatar")]
    [DataRow(true,"SaveAvatar")]
    [DataRow(false,"SaveAvatarDetail")]
    [DataRow(true,"SaveAvatarDetail")]
    [DataRow(false,"DeleteHolon")]
    [DataRow(true,"DeleteHolon")]
    [DataRow(false,"DeleteAvatarSoft")]
    [DataRow(true,"DeleteAvatarSoft")]
    [DataRow(false,"DeleteAvatarHard")]
    [DataRow(true,"DeleteAvatarHard")]
    public async Task LostFirstStorageReceiptPreservesNativeHashAndFencesV2(bool synchronous,string operation)
    {
        var (reader,contract)=await CreateActivatedProviderAsync();
        var provider=new LostStorageReceiptProvider(contract);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var fixture=Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON")
            ?? throw new InvalidOperationException("Retained fixture path required.");
        var path=Path.Combine(Path.GetDirectoryName(fixture)!,"storage-receipt-backup.db");
        if(File.Exists(path)) throw new InvalidOperationException("Existing receipt fixture requires reconciliation; refusing overwrite.");
        var backup=new SQLiteProvider($"Data Source={path};Pooling=False");
        var verified=false;
        try
        {
            Assert.IsFalse((await backup.ActivateProviderAsync()).IsError);
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings() }};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
            manager.RegisterProvider(provider);manager.RegisterProvider(backup);
            var stores=new[] {new EnumValue<ProviderType>(ProviderType.EthereumOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(stores).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(stores).IsError);
            var router=new OASISHyperDrive(manager);
            var id=Guid.NewGuid();
            var deleting=operation.StartsWith("Delete",StringComparison.Ordinal);
            if(deleting)
            {
                foreach(var store in new EthereumOrSqliteStore[] {new(reader),new(backup)})
                    await store.Seed(id,operation=="DeleteHolon");
            }
            IHolonBase payload=operation=="SaveAvatar" ? new Avatar {Id=id,Username="owned receipt avatar"}
                :operation=="SaveAvatarDetail" ? new AvatarDetail {Id=id,Username="owned receipt detail",XP=37}
                :new Holon {Id=id,Name="owned receipt holon"};
            var request=new StorageOperationRequest {Operation=operation.StartsWith("DeleteAvatar",StringComparison.Ordinal) ? "DeleteAvatar":operation,
                Payload=payload,HolonId=id,AvatarId=id,SoftDelete=operation!="DeleteAvatarHard" && operation!="DeleteHolon",
                PreferredProvider=ProviderType.EthereumOASIS,SaveChildren=false};
            async Task Check<T>()
            {
                var result=synchronous ? router.RouteRequest<T>(request):await router.RouteRequestAsync<T>(request);
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
                Assert.IsFalse(result.IsDeleted);
                var marker=result.Exception as EthereumStorageReceiptReconciliationException;
                Assert.IsNotNull(marker,result.Exception?.ToString());
                Assert.AreSame(provider.Cause,marker.InnerException);
                Assert.AreEqual(provider.SubmittedHash,marker.OriginalTransactionId);
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(marker));
            }
            if(operation.StartsWith("DeleteAvatar",StringComparison.Ordinal)) await Check<bool>();
            else if(operation=="SaveAvatar") await Check<IAvatar>();
            else if(operation=="SaveAvatarDetail") await Check<IAvatarDetail>();
            else await Check<IHolon>();
            Assert.AreEqual(1,provider.Polls);
            Assert.AreEqual(0L,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            var native=new Web3(new Account(PrivateKey,ChainId),Host);
            var receipt=await native.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(provider.SubmittedHash);
            Assert.AreEqual(BigInteger.One,receipt.Status.Value);
            if(operation=="DeleteHolon") Assert.IsNull((await reader.LoadHolonAsync(id,loadChildren:false)).Result);
            else if(operation=="DeleteAvatarHard")
            {
                Assert.IsNull((await reader.LoadAvatarAsync(id)).Result);
                Assert.IsNull((await reader.LoadAvatarDetailAsync(id)).Result);
            }
            else if(operation=="DeleteAvatarSoft")
            {
                var profile=(await reader.LoadAvatarAsync(id)).Result;
                var detail=(await reader.LoadAvatarDetailAsync(id)).Result;
                Assert.IsFalse(profile.IsActive);Assert.IsFalse(detail.IsActive);
                Assert.AreNotEqual(DateTime.MinValue,profile.DeletedDate);
                Assert.AreEqual(profile.DeletedDate,detail.DeletedDate);
            }
            else if(operation=="SaveAvatar") Assert.AreEqual(id,(await reader.LoadAvatarAsync(id)).Result.Id);
            else if(operation=="SaveAvatarDetail") Assert.AreEqual(37L,Convert.ToInt64((await reader.LoadAvatarDetailAsync(id)).Result.XP));
            else Assert.AreEqual(id,(await reader.LoadHolonAsync(id,loadChildren:false)).Result.Id);
            if(deleting && operation=="DeleteHolon") Assert.AreEqual("owned pre-delete",(await backup.LoadHolonAsync(id,loadChildren:false)).Result.Name);
            else Assert.IsNull((await backup.LoadHolonAsync(id,loadChildren:false)).Result);
            if(deleting && operation!="DeleteHolon")
            {
                var profile=(await backup.LoadAvatarAsync(id)).Result;var detail=(await backup.LoadAvatarDetailAsync(id)).Result;
                Assert.IsTrue(profile.IsActive);Assert.IsTrue(detail.IsActive);
                Assert.AreEqual(DateTime.MinValue,profile.DeletedDate);Assert.AreEqual(DateTime.MinValue,detail.DeletedDate);
                Assert.AreEqual(37L,Convert.ToInt64(detail.XP));
            }
            else
            {
                Assert.IsNull((await backup.LoadAvatarAsync(id)).Result);
                Assert.IsNull((await backup.LoadAvatarDetailAsync(id)).Result);
            }
            verified=true;
        }
        finally
        {
            backup.Dispose();
            if(verified) File.Delete(path); // Preserve failed post-dispatch evidence, never reset to green.
            await provider.DeActivateProviderAsync();await reader.DeActivateProviderAsync();
        }
    }

    private sealed class EthereumOrSqliteStore
    {
        private readonly IOASISStorageProvider _provider;
        public EthereumOrSqliteStore(IOASISStorageProvider provider) => _provider=provider;
        public async Task Seed(Guid id,bool holon)
        {
            if(holon) Assert.IsFalse((await _provider.SaveHolonAsync(new Holon {Id=id,Name="owned pre-delete"},saveChildren:false)).IsError);
            else
            {
                var avatar=await _provider.SaveAvatarAsync(new Avatar {Id=id,Username="owned pre-delete",IsActive=true,
                    AvatarType=new EnumValue<AvatarType>(AvatarType.User)});
                Assert.IsFalse(avatar.IsError,avatar.Message);
                var detail=await _provider.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="owned pre-delete",IsActive=true,XP=37});
                Assert.IsFalse(detail.IsError,detail.Message);
            }
        }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task HardAvatarDeleteRejectsCollidingDetailWithoutRemovingEitherRecord(bool synchronous)
    {
        var (provider,contract)=await CreateActivatedProviderAsync();
        try
        {
            var native=new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),contract);
            var id=Guid.NewGuid();var other=Guid.NewGuid();
            var key=NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(id.ToString());
            Assert.AreEqual(BigInteger.One,(await native.CreateAvatarRequestAndWaitForReceiptAsync(key,id.ToString(),"synthetic profile")).Status.Value);
            Assert.AreEqual(BigInteger.One,(await native.CreateAvatarDetailRequestAndWaitForReceiptAsync(key,other.ToString(),"synthetic conflicting detail")).Status.Value);
            var result=synchronous ? provider.DeleteAvatar(id,softDelete:false):await provider.DeleteAvatarAsync(id,softDelete:false);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);
            Assert.AreEqual(BigInteger.One,await native.GetAvatarsCountQueryAsync());
            Assert.AreEqual(BigInteger.One,await native.GetAvatarDetailsCountQueryAsync());
            Assert.AreEqual(id.ToString(),(await native.GetAvatarByIdQueryAsync(key)).ReturnValue1.AvatarId);
            Assert.AreEqual(other.ToString(),(await native.GetAvatarDetailByIdQueryAsync(key)).ReturnValue1.AvatarId);
        }
        finally {await provider.DeActivateProviderAsync();}
    }
}
