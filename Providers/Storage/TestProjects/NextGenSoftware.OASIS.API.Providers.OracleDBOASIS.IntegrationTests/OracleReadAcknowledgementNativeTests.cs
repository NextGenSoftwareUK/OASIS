using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeReadAcknowledgementsReportActualSuccessAndRootCounts(bool synchronous)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,"read-acknowledgements.pending.json");
        Assert.IsFalse(File.Exists(journal));var ids=new[]{Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()};
        var name="owned-read-ack-"+ids[0].ToString("N");const string marker="owned.read.ack";
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};IOASISStorageProvider storage=provider;
        await using var native=new OracleConnection(connection);await native.OpenAsync();
        var rows=new[]{("OASIS_AVATARS",ids[0]),("OASIS_AVATAR_DETAILS",ids[0]),("OASIS_HOLONS",ids[1]),("OASIS_HOLONS",ids[2])};
        foreach(var row in rows)Assert.IsNull(await Read(native,row.Item1,row.Item2));
        var bad=new List<string>();var originals=new Dictionary<(string,Guid),string>();
        void Check<T>(string label,OASISResult<T> result,bool failure=false)
        {
            if(failure){if(!result.IsError||result.IsLoaded||result.LoadedCount!=0)bad.Add(label+": failed read claimed loaded data");return;}
            if(result.IsError||result.Result is null){bad.Add(label+": missing successful payload");return;}
            var expected=result.Result is ISearchResults search?search.NumberOfResults:
                result.Result is System.Collections.IEnumerable values&&result.Result is not string?values.Cast<object>().Count():1;
            if(!result.IsLoaded||result.LoadedCount!=expected||result.IsSaved)
                bad.Add($"{label}: IsLoaded={result.IsLoaded},LoadedCount={result.LoadedCount},expected={expected},IsSaved={result.IsSaved}");
        }
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{state="before-owned-read-ack-fixture",ids,name}));
            var avatar=new Avatar{Id=ids[0],Username=name,Email=name+"@example.invalid",MetaData=new(){[marker]=name},
                VerificationToken=name+"-verify",ResetToken=name+"-reset",RefreshTokens=new(){new RefreshToken{Token=name+"-refresh",Expires=DateTime.UtcNow.AddHours(1)}},
                ProviderWallets=new(){[ProviderType.OracleDBOASIS]=new List<IProviderWallet>{new ProviderWallet{PublicKey=name+"-public",PrivateKey=name+"-private"}}}};
            var detail=new AvatarDetail{Id=ids[0],Username=name,Email=avatar.Email};
            var root=new Holon{Id=ids[1],Name=name,CreatedByAvatarId=ids[0],MetaData=new(){[marker]=name}};
            var child=new Holon{Id=ids[2],Name=name+"-child",ParentHolonId=ids[1],CreatedByAvatarId=ids[0],MetaData=new(){[marker]=name}};
            for(var version=1;version<=2;version++){
                Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);
                Assert.IsFalse((await provider.SaveHolonAsync(root,saveChildren:false)).IsError);Assert.IsFalse((await provider.SaveHolonAsync(child,saveChildren:false)).IsError);
            }
            foreach(var row in rows)originals[row]=(await Read(native,row.Item1,row.Item2))!;
            foreach(var version in new[]{0,1}){
                var tag=$"version{version}/";
                Check(tag+"avatar-id",synchronous?storage.LoadAvatar(ids[0],version):await storage.LoadAvatarAsync(ids[0],version));
                Check(tag+"avatar-key",synchronous?storage.LoadAvatarByProviderKey(ids[0].ToString(),version):await storage.LoadAvatarByProviderKeyAsync(ids[0].ToString(),version));
                Check(tag+"avatar-username",synchronous?storage.LoadAvatarByUsername(name,version):await storage.LoadAvatarByUsernameAsync(name,version));
                Check(tag+"avatar-email",synchronous?storage.LoadAvatarByEmail(avatar.Email,version):await storage.LoadAvatarByEmailAsync(avatar.Email,version));
                Check(tag+"detail-id",synchronous?storage.LoadAvatarDetail(ids[0],version):await storage.LoadAvatarDetailAsync(ids[0],version));
                Check(tag+"detail-username",synchronous?storage.LoadAvatarDetailByUsername(name,version):await storage.LoadAvatarDetailByUsernameAsync(name,version));
                Check(tag+"detail-email",synchronous?storage.LoadAvatarDetailByEmail(avatar.Email,version):await storage.LoadAvatarDetailByEmailAsync(avatar.Email,version));
                Check(tag+"holon-id",synchronous?storage.LoadHolon(ids[1],loadChildrenFromProvider:true,version:version):await storage.LoadHolonAsync(ids[1],loadChildrenFromProvider:true,version:version));
                Check(tag+"holon-key",synchronous?storage.LoadHolon(ids[1].ToString(),version:version):await storage.LoadHolonAsync(ids[1].ToString(),version:version));
                Check(tag+"avatars",synchronous?storage.LoadAllAvatars(version):await storage.LoadAllAvatarsAsync(version));
                Check(tag+"details",synchronous?storage.LoadAllAvatarDetails(version):await storage.LoadAllAvatarDetailsAsync(version));
                Check(tag+"holons",synchronous?storage.LoadAllHolons(loadChildrenFromProvider:true,version:version):await storage.LoadAllHolonsAsync(loadChildrenFromProvider:true,version:version));
                Check(tag+"parent-id",synchronous?storage.LoadHolonsForParent(ids[1],version:version):await storage.LoadHolonsForParentAsync(ids[1],version:version));
                Check(tag+"parent-key",synchronous?storage.LoadHolonsForParent(ids[1].ToString(),version:version):await storage.LoadHolonsForParentAsync(ids[1].ToString(),version:version));
                Check(tag+"metadata",synchronous?storage.LoadHolonsByMetaData(marker,name,version:version):await storage.LoadHolonsByMetaDataAsync(marker,name,version:version));
                Check(tag+"metadata-map",synchronous?storage.LoadHolonsByMetaData(new(){[marker]=name},MetaKeyValuePairMatchMode.All,version:version):await storage.LoadHolonsByMetaDataAsync(new(){[marker]=name},MetaKeyValuePairMatchMode.All,version:version));
                Check(tag+"empty-metadata",synchronous?storage.LoadHolonsByMetaData(marker,name+"-absent",version:version):await storage.LoadHolonsByMetaDataAsync(marker,name+"-absent",version:version));
                Check(tag+"export",synchronous?storage.ExportAll(version):await storage.ExportAllAsync(version));
                Check(tag+"export-owner",synchronous?storage.ExportAllDataForAvatarById(ids[0],version):await storage.ExportAllDataForAvatarByIdAsync(ids[0],version));
                Check(tag+"export-email",synchronous?storage.ExportAllDataForAvatarByEmail(avatar.Email,version):await storage.ExportAllDataForAvatarByEmailAsync(avatar.Email,version));
                Check(tag+"export-username",synchronous?storage.ExportAllDataForAvatarByUsername(name,version):await storage.ExportAllDataForAvatarByUsernameAsync(name,version));
                var search=new SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){[marker]=name}};
                Check(tag+"search",synchronous?storage.Search(search,version:version):await storage.SearchAsync(search,version:version));
            }
            Check("public-wallet",synchronous?storage.LoadAvatarByPublicKey(name+"-public"):await storage.LoadAvatarByPublicKeyAsync(name+"-public"));
            Check("private-wallet",synchronous?storage.LoadAvatarByPrivateKey(name+"-private"):await storage.LoadAvatarByPrivateKeyAsync(name+"-private"));
            Check("verification",synchronous?storage.LoadAvatarByVerificationToken(name+"-verify"):await storage.LoadAvatarByVerificationTokenAsync(name+"-verify"));
            Check("reset",synchronous?storage.LoadAvatarByResetToken(name+"-reset"):await storage.LoadAvatarByResetTokenAsync(name+"-reset"));
            Check("refresh",synchronous?storage.LoadAvatarByRefreshToken(name+"-refresh"):await storage.LoadAvatarByRefreshTokenAsync(name+"-refresh"));
            Check("missing-id",synchronous?storage.LoadAvatar(Guid.NewGuid()):await storage.LoadAvatarAsync(Guid.NewGuid()),failure:true);
            foreach(var row in rows){
                await using var cleanup=new OracleCommand($"DELETE FROM {row.Item1} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",native){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=row.Item2.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=originals[row];
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(native,row.Item1,row.Item2));
            }
            File.Delete(journal);
        }
        finally{await provider.DeActivateProviderAsync();}
        Assert.AreEqual(0,bad.Count,string.Join(Environment.NewLine,bad));
    }
}
