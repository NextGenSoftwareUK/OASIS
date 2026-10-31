using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [TestMethod]
    public async Task NativeWalletLookupsUseCurrentAuthorityAndRejectDuplicates()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,"wallet-credentials.pending.json");
        Assert.IsFalse(File.Exists(journal));var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};var name="owned-wallet-"+ids[0].ToString("N");
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};IOASISStorageProvider storage=provider;
        void SetWallet(Avatar avatar,string suffix)=>avatar.ProviderWallets=new(){[ProviderType.OracleDBOASIS]=new List<IProviderWallet>{new ProviderWallet{PublicKey=name+"-public-"+suffix,PrivateKey=name+"-private-"+suffix}}};
        async Task Check(string suffix,Guid? expected,string? error=null){
            var results=new[]{storage.LoadAvatarByPublicKey(name+"-public-"+suffix),await storage.LoadAvatarByPublicKeyAsync(name+"-public-"+suffix),storage.LoadAvatarByPrivateKey(name+"-private-"+suffix),await storage.LoadAvatarByPrivateKeyAsync(name+"-private-"+suffix)};
            foreach(var result in results){Assert.AreEqual(!expected.HasValue,result.IsError,result.Exception?.GetType().Name);if(expected.HasValue)Assert.AreEqual(expected,result.Result!.Id);else {Assert.IsNull(result.Result);Assert.AreEqual(error,result.ErrorCode);}}
        }
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();foreach(var id in ids)Assert.IsNull(await Read(verifier,"OASIS_AVATARS",id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,name,state="before-owned-wallet-credential-fixture"}));file.Flush(true);}
            var first=new Avatar{Id=ids[0],Username=name,Email=name+"@example.invalid",IsActive=true};SetWallet(first,"old");
            Assert.IsFalse((await provider.SaveAvatarAsync(first)).IsError);await Check("old",first.Id);
            SetWallet(first,"new");Assert.IsFalse((await provider.SaveAvatarAsync(first)).IsError);
            await Check("old",null,OASISResultErrorCodes.RecordNotFound);await Check("new",first.Id);
            var historical=await provider.LoadAvatarAsync(first.Id,1);Assert.IsFalse(historical.IsError);Assert.AreEqual(name+"-public-old",historical.Result.ProviderWallets[ProviderType.OracleDBOASIS][0].PublicKey);
            var forbidden=await storage.LoadAvatarByPublicKeyAsync(name+"-public-old",1);Assert.IsTrue(forbidden.IsError);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",forbidden.ErrorCode);Assert.IsNull(forbidden.Result);
            var second=new Avatar{Id=ids[1],Username=name+"-second",Email=name+"-second@example.invalid",IsActive=true};SetWallet(second,"new");Assert.IsFalse((await provider.SaveAvatarAsync(second)).IsError);
            await Check("new",null,OASISResultErrorCodes.AmbiguousIdentity);
            foreach(var id in ids){var expected=await Read(verifier,"OASIS_AVATARS",id);Assert.IsNotNull(expected);
                await using var cleanup=new OracleCommand("DELETE FROM OASIS_AVATARS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=expected;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,"OASIS_AVATARS",id));}
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
