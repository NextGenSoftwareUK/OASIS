using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests;

/// <summary>Real retained CSS pod, synthetic data, anonymous loopback transport only—not OIDC proof.</summary>
public class RetainedSolidPodTests
{
    private static async Task WriteJournalAsync(string path,object intent)
    {
        // Create-only and flush-to-disk before any native mutation; a pending original is never overwritten.
        using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
        await file.WriteAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(intent)));
        file.Flush(flushToDisk:true);
    }
    private static async Task EnsureContainersAsync(HttpClient client,string root)
    {
        foreach(var path in new[]{"","oasis/","oasis/avatars/","oasis/avatar-details/","oasis/holons/"}){
            var uri=new Uri(new Uri(root),path);using var head=await client.SendAsync(new HttpRequestMessage(HttpMethod.Head,uri));
            if(head.StatusCode==HttpStatusCode.NotFound){
                using var create=new HttpRequestMessage(HttpMethod.Put,uri){Content=new StringContent("",Encoding.UTF8,"text/turtle")};
                create.Headers.TryAddWithoutValidation("Link","<http://www.w3.org/ns/ldp#BasicContainer>; rel=\"type\"");
                using var made=await client.SendAsync(create);Assert.True(made.IsSuccessStatusCode,$"Container setup status {(int)made.StatusCode}; keep original journal.");
            }else Assert.True(head.IsSuccessStatusCode,$"Container inspection status {(int)head.StatusCode}.");
        }
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task NativeGraphQueriesImportAndExport(bool synchronous)
    {
        var root=Environment.GetEnvironmentVariable("OASIS_SOLID_NATIVE_ROOT")!;
        Assert.Equal("http://127.0.0.1:13000/native-fixture/",root);
        var journal=Environment.GetEnvironmentVariable("OASIS_SOLID_NATIVE_JOURNAL")!;
        Assert.False(File.Exists(journal),"Preserve and reconcile the original native fixture before further writes.");
        var key=ProtectedData.Unprotect(await File.ReadAllBytesAsync(Environment.GetEnvironmentVariable("OASIS_SOLID_NATIVE_KEY_PATH")!),null,DataProtectionScope.CurrentUser);
        try{
            using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(10)};
            using var provider=new SOLIDOASIS(root,"",key);
            var owner=Guid.NewGuid();
            var grand=new Holon{Id=Guid.NewGuid(),Name="native-private-graph-grand",Version=9,CreatedByAvatarId=owner};
            var child=new Holon{Id=Guid.NewGuid(),Name="native-private-graph-child",Version=2,CreatedByAvatarId=owner,Children=new List<IHolon>{grand}};
            IHolon graph=new Holon{Id=Guid.NewGuid(),Name="native-private-graph-root",Version=5,ParentHolonId=owner,CreatedByAvatarId=owner,
                MetaData=new(){["tier"]="root"},Children=new List<IHolon>{child}};
            var rows=new IHolon[]{graph,child,grand};
            var resources=rows.Select(row=>new Uri(new Uri(root),$"oasis/holons/{row.Id}.ttl")).ToArray();
            await WriteJournalAsync(journal,new{root,synchronous,resources=resources.Select(uri=>uri.AbsoluteUri).ToArray(),state="before-native-graph"});
            await EnsureContainersAsync(client,root);
            foreach(var uri in resources){using var absent=await client.GetAsync(uri);Assert.Equal(HttpStatusCode.NotFound,absent.StatusCode);}
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var saved=synchronous?provider.SaveHolon(graph):await provider.SaveHolonAsync(graph);
            Assert.False(saved.IsError,saved.Exception?.ToString());
            Assert.Equal(graph.Id,child.ParentHolonId);Assert.Equal(child.Id,grand.ParentHolonId);
            foreach(var row in rows)Assert.Equal(row.Id.ToString(),row.ProviderUniqueStorageKey[ProviderType.SOLIDOASIS]);
            foreach(var uri in resources){using var record=await client.GetAsync(uri);Assert.True(record.IsSuccessStatusCode);var rdf=await record.Content.ReadAsStringAsync();Assert.DoesNotContain("native-private",rdf);Assert.Contains("oasis-aes256gcm-v1:",rdf);}
            var linked=synchronous?provider.LoadHolon(graph.Id,loadChildrenFromProvider:true):await provider.LoadHolonAsync(graph.Id,loadChildrenFromProvider:true);
            Assert.False(linked.IsError,linked.Exception?.ToString());
            var linkedChild=Assert.Single(linked.Result.Children);Assert.Equal(child.Id,linkedChild.Id);Assert.Equal(2,linkedChild.Version);
            Assert.Equal(grand.Id,Assert.Single(linkedChild.Children).Id);
            var limited=await provider.LoadHolonAsync(graph.Id,maxChildDepth:1,loadChildrenFromProvider:true);
            Assert.False(limited.IsError,limited.Message);Assert.Empty(Assert.Single(limited.Result.Children).Children);
            var metadata=synchronous?provider.LoadHolonsByMetaData("tier","root",loadChildrenFromProvider:true):await provider.LoadHolonsByMetaDataAsync("tier","root",loadChildrenFromProvider:true);
            Assert.False(metadata.IsError,metadata.Exception?.ToString());Assert.Equal(graph.Id,Assert.Single(metadata.Result).Id);
            Assert.Equal(child.Id,Assert.Single(Assert.Single(metadata.Result).Children).Id);
            var parents=await provider.LoadHolonsForParentAsync(graph.Id,loadChildren:false);
            Assert.False(parents.IsError,parents.Message);Assert.Equal(child.Id,Assert.Single(parents.Result).Id);
            var byKey=await provider.LoadHolonAsync(graph.Id.ToString(),loadChildren:false);
            Assert.False(byKey.IsError,byKey.Message);Assert.Equal(graph.Id,byKey.Result.Id);
            var query=new SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){["tier"]="root"}};
            var searched=await provider.SearchAsync(query,loadChildren:false);
            Assert.False(searched.IsError,searched.Exception?.ToString());Assert.Equal(graph.Id,Assert.Single(searched.Result.SearchResultHolons).Id);
            child.Name="native-private-imported-child";
            var imported=synchronous?provider.Import(new[]{graph}):await provider.ImportAsync(new[]{graph});
            Assert.False(imported.IsError,imported.Exception?.ToString());Assert.True(imported.Result);
            var readChild=await provider.LoadHolonAsync(child.Id,loadChildren:false);
            Assert.False(readChild.IsError,readChild.Message);Assert.Equal(child.Name,readChild.Result.Name);
            var exported=synchronous?provider.ExportAllDataForAvatarById(owner):await provider.ExportAllDataForAvatarByIdAsync(owner);
            Assert.False(exported.IsError,exported.Exception?.ToString());Assert.Equal(3,exported.Result.Count());
            Assert.Contains(exported.Result,row=>row.Id==child.Id&&row.Name==child.Name);
            foreach(var row in rows.Reverse()){
                var deleted=synchronous?provider.DeleteHolon(row.Id):await provider.DeleteHolonAsync(row.Id);
                Assert.False(deleted.IsError,deleted.Exception?.ToString());Assert.True(deleted.IsDeleted);
            }
            foreach(var uri in resources){using var absent=await client.GetAsync(uri);Assert.Equal(HttpStatusCode.NotFound,absent.StatusCode);}
            File.Delete(journal);
        }finally{CryptographicOperations.ZeroMemory(key);}
    }
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
            await WriteJournalAsync(journal,new{root,synchronous,resources=resources.Select(uri=>uri.AbsoluteUri).ToArray(),state="before-native-setup"});
            await EnsureContainersAsync(client,root!);
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
