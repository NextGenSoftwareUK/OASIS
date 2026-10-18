using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests;

/// <summary>Real retained CSS pod, synthetic data, anonymous loopback transport only—not OIDC proof.</summary>
public class RetainedSolidPodTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task EncryptedNativePodRecordsCollectionsAndLookups(bool synchronous)
    {
        var root=Environment.GetEnvironmentVariable("OASIS_SOLID_NATIVE_ROOT");
        Assert.Equal("http://127.0.0.1:13000/native-fixture/",root);
        var keyPath=Environment.GetEnvironmentVariable("OASIS_SOLID_NATIVE_KEY_PATH")!;
        var journal=Environment.GetEnvironmentVariable("OASIS_SOLID_NATIVE_JOURNAL")!;
        Assert.False(File.Exists(journal),"Original pending pod fixture requires reconciliation; never replay it.");
        Assert.True(OperatingSystem.IsWindows(),"Retained key uses Windows CurrentUser DPAPI.");
        var key=ProtectedData.Unprotect(await File.ReadAllBytesAsync(keyPath),null,DataProtectionScope.CurrentUser);
        try{
            using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(10)};
            using var provider=new SOLIDOASIS(root!,"",key);
            var avatarId=Guid.NewGuid();var detailId=avatarId;var holonId=Guid.NewGuid();
            var resources=new[]{new Uri(new Uri(root!),$"oasis/avatars/{avatarId}.ttl"),new Uri(new Uri(root!),$"oasis/avatar-details/{detailId}.ttl"),new Uri(new Uri(root!),$"oasis/holons/{holonId}.ttl")};
            // Retain intended identities BEFORE any setup/write. Durable key remains outside this journal.
            await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{root,synchronous,resources=resources.Select(uri=>uri.AbsoluteUri).ToArray(),state="before-native-setup"}));
            foreach(var path in new[]{"","oasis/","oasis/avatars/","oasis/avatar-details/","oasis/holons/"}){
                var uri=new Uri(new Uri(root!),path);using var head=await client.SendAsync(new HttpRequestMessage(HttpMethod.Head,uri));
                if(head.StatusCode==HttpStatusCode.NotFound){
                    using var create=new HttpRequestMessage(HttpMethod.Put,uri){Content=new StringContent("",Encoding.UTF8,"text/turtle")};
                    create.Headers.TryAddWithoutValidation("Link","<http://www.w3.org/ns/ldp#BasicContainer>; rel=\"type\"");
                    using var made=await client.SendAsync(create);Assert.True(made.IsSuccessStatusCode,$"Container setup status {(int)made.StatusCode}; keep original journal.");
                }else Assert.True(head.IsSuccessStatusCode,$"Container inspection status {(int)head.StatusCode}.");
            }
            foreach(var uri in resources){using var absent=await client.GetAsync(uri);Assert.Equal(HttpStatusCode.NotFound,absent.StatusCode);}
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var initial=await provider.LoadAllAvatarsAsync();Assert.False(initial.IsError,initial.Exception?.ToString());Assert.Empty(initial.Result);
            IAvatar avatar=new Avatar{Id=avatarId,Username="native-private-canary",Email="native-canary@example.invalid",Password="native-private-password-canary",Version=4};
            IAvatarDetail detail=new AvatarDetail{Id=detailId,Username=avatar.Username,Email=avatar.Email,Description="native-private-detail-canary",Version=3};
            IHolon holon=new Holon{Id=holonId,Name="native-private-holon-canary",MetaData=new(){["native"]="native-private-metadata-canary"},Version=2};
            var a=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);Assert.False(a.IsError,a.Exception?.ToString());
            var d=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);Assert.False(d.IsError,d.Exception?.ToString());
            var h=synchronous?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);Assert.False(h.IsError,h.Exception?.ToString());
            foreach(var uri in resources){using var actual=await client.GetAsync(uri);Assert.True(actual.IsSuccessStatusCode);var rdf=await actual.Content.ReadAsStringAsync();Assert.DoesNotContain("native-private",rdf);Assert.DoesNotContain("native-canary@example.invalid",rdf);Assert.Contains("oasis-aes256gcm-v1:",rdf);}
            var loaded=synchronous?provider.LoadAvatar(avatarId):await provider.LoadAvatarAsync(avatarId);Assert.False(loaded.IsError,loaded.Message);Assert.Equal(avatar.Password,loaded.Result.Password);
            var details=synchronous?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync();Assert.False(details.IsError,details.Message);Assert.Equal(detail.Description,Assert.Single(details.Result).Description);
            var holons=synchronous?provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false);Assert.False(holons.IsError,holons.Message);Assert.Equal(holonId,Assert.Single(holons.Result).Id);
            var byEmail=await provider.LoadAvatarByEmailAsync(" NATIVE-CANARY@EXAMPLE.INVALID ");Assert.False(byEmail.IsError,byEmail.Message);Assert.Equal(avatarId,byEmail.Result.Id);
            var byName=await provider.LoadAvatarDetailByUsernameAsync(" NATIVE-PRIVATE-CANARY ");Assert.False(byName.IsError,byName.Message);Assert.Equal(detailId,byName.Result.Id);
            var soft=synchronous?provider.DeleteAvatar(avatarId):await provider.DeleteAvatarAsync(avatarId);
            Assert.False(soft.IsError,soft.Exception?.ToString());Assert.True(soft.Result);Assert.True(soft.IsDeleted);
            var deletedAvatar=await provider.LoadAvatarAsync(avatarId);Assert.False(deletedAvatar.IsError,deletedAvatar.Message);
            var deletedDetail=await provider.LoadAvatarDetailAsync(detailId);Assert.False(deletedDetail.IsError,deletedDetail.Message);
            Assert.False(deletedAvatar.Result.IsActive);Assert.False(deletedDetail.Result.IsActive);
            Assert.NotEqual(DateTime.MinValue,deletedAvatar.Result.DeletedDate);
            Assert.Equal(deletedAvatar.Result.DeletedDate,deletedDetail.Result.DeletedDate);
            Assert.Equal(avatar.Password,deletedAvatar.Result.Password);Assert.Equal(detail.Description,deletedDetail.Result.Description);
            var removed=synchronous?provider.DeleteAvatar(avatarId,false):await provider.DeleteAvatarAsync(avatarId,false);
            Assert.False(removed.IsError,removed.Exception?.ToString());Assert.True(removed.Result);Assert.True(removed.IsDeleted);
            var removedHolon=synchronous?provider.DeleteHolon(holonId.ToString()):await provider.DeleteHolonAsync(holonId.ToString());
            Assert.False(removedHolon.IsError,removedHolon.Exception?.ToString());Assert.Equal(holonId,removedHolon.Result.Id);Assert.True(removedHolon.IsDeleted);
            // Provider deletes must be independently visible as absent; never raw-delete on failed assertions.
            foreach(var uri in resources){using var absent=await client.GetAsync(uri);Assert.Equal(HttpStatusCode.NotFound,absent.StatusCode);}
            var empty=await provider.LoadAllAvatarsAsync();Assert.False(empty.IsError);Assert.Empty(empty.Result);
            File.Delete(journal);
        }finally{CryptographicOperations.ZeroMemory(key);}
    }
}
