using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;
using SqlContext=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context.DataContext;
using Repositories=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifySqliteWalletCommitFenceAsync(SQLLiteDBOASIS.SQLLiteDBOASIS healthy)
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(SQLLiteDBOASIS.SQLLiteDBOASIS);
        var db=(DbContext)type.GetField("_appDataContext",flags)!.GetValue(healthy)!;
        var connection=db.Database.GetDbConnection().ConnectionString;
        foreach(var asynchronous in new[] {false,true})
        foreach(var batch in new[] {false,true})
        foreach(var existing in new[] {false,true})
        {
            var ids=Enumerable.Range(0,batch ? 2:1).Select(_=>Guid.NewGuid()).ToArray();
            Dictionary<Core.Enums.ProviderType,List<IProviderWallet>> Wallets(string address)=>new() {
                [Core.Enums.ProviderType.SolanaOASIS]=ids.Select(id=>(IProviderWallet)new Core.Objects.ProviderWallet {
                    WalletId=Guid.NewGuid(),AvatarId=id,ProviderType=Core.Enums.ProviderType.SolanaOASIS,WalletAddress=address}).ToList()};
            foreach(var id in ids)
                Assert.IsFalse((await healthy.SaveAvatarAsync(new Avatar {Id=id,IsActive=true,Username="wallet-commit-"+id.ToString("N"),AvatarType=new EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
            if(existing) Assert.IsFalse((await healthy.SaveProviderWalletsAsync(Wallets("before committed update"))).IsError);
            var cause=new IOException("Owned wallet notification failure AFTER real commit.");
            IInterceptor interceptor=batch ? new AfterTransactionCommitFault(cause):new AfterCommitFault(cause);
            using var context=new CommitFaultContext(connection,interceptor);
            using var provider=new SQLLiteDBOASIS.SQLLiteDBOASIS(connection);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            // Test-only interceptor injection: all shipping repositories share the SAME existing database/context.
            // No production persistence implementation is overridden or copied.
            var original=(DbContext)type.GetField("_appDataContext",flags)!.GetValue(provider)!;
            type.GetField("_appDataContext",flags)!.SetValue(provider,context);
            type.GetField("_avatarRepository",flags)!.SetValue(provider,new Repositories.AvatarRepository(context));
            type.GetField("_avatarDetailRepository",flags)!.SetValue(provider,new Repositories.AvatarDetailRepository(context));
            type.GetField("_holonRepository",flags)!.SetValue(provider,new Repositories.HolonRepository(context));
            original.Dispose();
            var wallets=Wallets("actually committed wallet");
            var result=batch ? asynchronous ? await provider.SaveProviderWalletsAsync(wallets):provider.SaveProviderWallets(wallets)
                : asynchronous ? await provider.SaveProviderWalletsForAvatarByIdAsync(ids[0],wallets):provider.SaveProviderWalletsForAvatarById(ids[0],wallets);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
            var failure=result.Exception as SQLLiteDBOASIS.SqliteMutationReconciliationException;Assert.IsNotNull(failure);
            CollectionAssert.AreEquivalent(ids,failure.RecordIds.ToArray());Assert.AreSame(cause,failure.InnerException);
            using var control=new SqlContext(connection);
            foreach(var id in ids)
            {
                var row=await control.AvatarWalletStores.AsNoTracking().SingleAsync(row=>row.AvatarId==id.ToString());
                StringAssert.Contains(row.WalletsJson,"actually committed wallet");
                Assert.IsFalse(row.WalletsJson.Contains("before committed update"));
            }
        }
    }
}
