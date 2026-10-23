using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class RedisOwnedKeyDeletionNativeTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("OwnedNativeKeyDeletion")]
    public async Task InvalidNativeKeyCannotDeleteExistingUsernameOrAvatarDetail(bool sync)
    {
        const string endpoint="127.0.0.1:18679";
        Assert.AreEqual(endpoint,Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_ENDPOINT"),"Explicit retained native test admission required.");
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_WORK")??throw new InvalidOperationException("Explicit proof directory required."));
        Assert.AreEqual(Path.GetFullPath("C:/Source/OASIS/TestResults/RedisCurrent"),work);
        Directory.CreateDirectory(work);
        var stem=Path.Combine(work,sync?"key-guard-sync":"key-guard-async");
        foreach(var suffix in new[]{".pending.json",".resolved.json",".snapshot.json"})Assert.IsFalse(File.Exists(stem+suffix),"Preserve original fixture evidence before rerun.");
        var id=Guid.NewGuid();var username="owned-redis-key-"+id.ToString("N");var email=username+"@example.invalid";
        var keys=new RedisKey[]{$"avatar:{id}",$"avatar-detail:{id}","avatar-by-username:"+username,"avatar-by-email:"+email,"detail-by-username:"+username,"detail-by-email:"+email};
        var options=ConfigurationOptions.Parse(endpoint);options.AbortOnConnectFail=true;options.ConnectRetry=0;options.ConnectTimeout=3000;options.SyncTimeout=3000;options.BacklogPolicy=BacklogPolicy.FailFast;
        var password=Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_PASSWORD");
        Assert.IsFalse(string.IsNullOrWhiteSpace(password),"Existing protected native test credential required.");
        options.Password=password;
        using var verifier=await ConnectionMultiplexer.ConnectAsync(options);var db=verifier.GetDatabase();
        Assert.IsTrue((await db.StringGetAsync(keys)).All(value=>value.IsNull),"No existing key may be claimed as a new fixture.");
        static string[] Members(RedisValue[] values)=>values.Select(value=>Convert.ToBase64String((byte[]?)value??Array.Empty<byte>())).OrderBy(value=>value,StringComparer.Ordinal).ToArray();
        var originalMembers=Members(await db.SetMembersAsync("avatars:all"));
        Assert.IsFalse(await db.SetContainsAsync("avatars:all",id.ToString()));
        using(var intent=new FileStream(stem+".pending.json",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            JsonSerializer.Serialize(intent,new{owner="owned-redis-key-deletion-v1",id,username,keys=keys.Select(key=>key.ToString()).ToArray(),endpoint,
                sharedSet="avatars:all",ownedSetMember=id.ToString(),originalMembersHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(originalMembers))))});intent.Flush(true);
        }
        var provider=new RedisOASIS(options.ToString(includePassword:true));
        try{
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Exception?.ToString());
            var profile=new Avatar{Id=id,Username=username,Email=email,IsActive=true,MetaData=new(){{"owned-native-fixture",username}}};
            var detail=new AvatarDetail{Id=id,Username=username,Email=email,IsActive=true,XP=321,Karma=29};
            detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name="owned preserved inventory"});
            var saved=sync?provider.SaveAvatar(profile):await provider.SaveAvatarAsync(profile);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            var savedDetail=sync?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(savedDetail.IsError,savedDetail.Exception?.ToString());
            var before=await db.StringGetAsync(keys);Assert.IsTrue(before.All(value=>value.HasValue),"Both records and all four identity indexes must exist.");
            Assert.IsTrue(await db.SetContainsAsync("avatars:all",id.ToString()));
            using(var snapshot=new FileStream(stem+".snapshot.json",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                JsonSerializer.Serialize(snapshot,keys.Select((key,index)=>new{key=key.ToString(),value=before[index].ToString()}).ToArray());snapshot.Flush(true);
            }
            var rejected=sync?provider.DeleteAvatar(username,false):await provider.DeleteAvatarAsync(username,false);
            Assert.IsTrue(rejected.IsError);Assert.IsInstanceOfType<ArgumentException>(rejected.Exception);Assert.IsFalse(rejected.Result);Assert.IsFalse(rejected.IsDeleted);Assert.AreEqual(0,rejected.DeletedCount);
            var after=await db.StringGetAsync(keys);CollectionAssert.AreEqual(before,after,"Invalid provider-key deletion must not change a record or index.");
            Assert.IsTrue(await db.SetContainsAsync("avatars:all",id.ToString()),"Invalid provider-key deletion cannot remove shared-set membership.");
            var loaded=await provider.LoadAvatarDetailAsync(id);Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(321L,loaded.Result.XP);Assert.AreEqual(29L,loaded.Result.Karma);Assert.AreEqual("owned preserved inventory",loaded.Result.Inventory.Single().Name);
            var cleanup=db.CreateTransaction();foreach(var pair in keys.Select((key,index)=>(key,value:before[index])))Assert.IsTrue(cleanup.AddCondition(Condition.StringEqual(pair.key,pair.value))!=null);
            Assert.IsNotNull(cleanup.AddCondition(Condition.SetContains("avatars:all",id.ToString())));
            var deleted=keys.Select(key=>cleanup.KeyDeleteAsync(key)).ToArray();
            var removedMember=cleanup.SetRemoveAsync("avatars:all",id.ToString());
            Assert.IsTrue(await cleanup.ExecuteAsync(),"Exact original fixture bytes must still match before cleanup.");
            Assert.IsTrue((await Task.WhenAll(deleted)).All(value=>value));
            Assert.IsTrue(await removedMember);
            Assert.IsTrue((await db.StringGetAsync(keys)).All(value=>value.IsNull),"Acknowledged cleanup requires actual absent readback.");
            Assert.IsFalse(await db.SetContainsAsync("avatars:all",id.ToString()));
            CollectionAssert.AreEqual(originalMembers,Members(await db.SetMembersAsync("avatars:all")),"Cleanup must preserve every foreign set member.");
            File.Move(stem+".pending.json",stem+".resolved.json");
        }finally{await provider.DeActivateProviderAsync();}
    }
}
