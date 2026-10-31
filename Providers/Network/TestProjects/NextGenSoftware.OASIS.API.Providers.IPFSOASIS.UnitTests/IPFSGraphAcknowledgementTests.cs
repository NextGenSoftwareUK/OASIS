using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Ipfs.Http;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public class IPFSGraphAcknowledgementTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task SelectedGraphCountIncludesPersistedChildAndSurvivesSingleResultProjection(bool sync,bool batch)
    {
        var checkout=new DirectoryInfo(AppContext.BaseDirectory);
        while(checkout!=null&&!Directory.Exists(Path.Combine(checkout.FullName,"Scripts","TestHosts")))checkout=checkout.Parent;
        Assert.NotNull(checkout);
        var folder=Path.Combine(checkout!.FullName,"TestResults","IPFSGraphAcknowledgement");Directory.CreateDirectory(folder);
        var path=Path.Combine(folder,"owned-dna.json");Assert.False(File.Exists(path));
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {StorageProviders=new StorageProviderSettings {
            IPFSOASIS=new IPFSOASISSettings {ConnectionString="http://127.0.0.1:1"}}}};
        var provider=new IPFSOASIS(dna,path);
        var key=RandomNumberGenerator.GetBytes(32);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(IPFSOASIS).GetField("_storageKeyResolver",flags)!.SetValue(provider,(Func<byte[]>)(()=> (byte[])key.Clone()));
        typeof(IPFSOASIS).GetField("_ipfsClient",flags)!.SetValue(provider,new IpfsClient("http://127.0.0.1:1"));
        var transport=new Transport();using var http=new HttpClient(transport);
        var api=typeof(IpfsClient).GetField("api",BindingFlags.Static|BindingFlags.NonPublic)!;var original=api.GetValue(null);
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        api.SetValue(null,http);
        try
        {
            var root=new Holon {Id=Guid.NewGuid()};var child=new Holon {Id=Guid.NewGuid()};root.Children.Add(child);
            if(batch){
                var result=sync?provider.SaveHolons(new[]{root}):await provider.SaveHolonsAsync(new[]{root});
                Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsSaved);
                Assert.Single(result.Result);Assert.Equal(2,result.SavedCount);
            }else{
                var result=sync?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
                Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsSaved);
                Assert.Same(root,result.Result);Assert.Equal(2,result.SavedCount);
            }
            Assert.Equal(4,transport.Adds);Assert.Equal(root.Id,child.ParentHolonId);

            // Acknowledging deletion must describe the committed tombstone, on both facades.
            var deleted = sync ? provider.DeleteHolon(child.Id) : await provider.DeleteHolonAsync(child.Id);
            Assert.False(deleted.IsError, deleted.Exception?.ToString());
            Assert.True(deleted.IsDeleted);
            Assert.Equal(1, deleted.DeletedCount);
            Assert.True(deleted.Result.IsDeleted);
            Assert.NotEqual(default, deleted.Result.DeletedDate);
            Assert.False(deleted.Result.IsActive);

            // Tombstones remain point-readable, but are no longer live parent members.
            var children = sync ? provider.LoadHolonsForParent(root.Id, loadChildren:false)
                : await provider.LoadHolonsForParentAsync(root.Id, loadChildren:false);
            Assert.False(children.IsError);
            Assert.DoesNotContain(children.Result, item => item.Id == child.Id);

            // Avatar logical removal must also acknowledge its durable catalogue change.
            var avatar = new Avatar { Id=Guid.NewGuid(), Username="delete-owner", Email="delete@example.invalid" };
            var savedAvatar = await provider.SaveAvatarAsync(avatar);
            Assert.False(savedAvatar.IsError);
            var removed = sync ? provider.DeleteAvatar(avatar.Id,softDelete:false)
                : await provider.DeleteAvatarAsync(avatar.Id,softDelete:false);
            Assert.False(removed.IsError);
            Assert.True(removed.Result);
            Assert.True(removed.IsDeleted);
            Assert.Equal(1,removed.DeletedCount);
        }
        finally{
            api.SetValue(null,original);CryptographicOperations.ZeroMemory(key);OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;
            if(File.Exists(path))File.Delete(path); // Exact owned mock configuration; no native state.
        }
    }

    private sealed class Transport : HttpMessageHandler
    {
        public int Adds;
        private readonly Dictionary<string,string> stored=new();
        private static readonly string[] Cids={"Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdk","QmPcSpfsigLFxjKLHvhKtBqiwtjmtHViHh9G4Hi8vdriSP",
            "QmRqPMYJEEsSvcDNTGsuBHp5s5EZsrVDJbs26BYGkRti9D","QmXuu1N91m5dR2oh3FzLZWpPYgNgPQuCxp7EMHGtpAJD9D",
            "Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdj", "Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdi",
            "Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdh", "Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdg",
            "Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdf"};
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            if(request.RequestUri!.AbsolutePath.EndsWith("/cat")){
                var argument=request.RequestUri.Query.TrimStart('?').Split('&').Single(item=>item.StartsWith("arg="));
                var cid=Uri.UnescapeDataString(argument.Substring(4));
                return new(HttpStatusCode.OK){Content=new StringContent(stored[cid])};
            }
            Assert.EndsWith("/add",request.RequestUri.AbsolutePath);
            var parts=Assert.IsType<MultipartFormDataContent>(request.Content);
            var payload=await Assert.Single(parts).ReadAsStringAsync(token);
            var selected=Cids[Adds++];stored[selected]=payload;
            return new(HttpStatusCode.OK){Content=new StringContent("{\"Name\":\"\",\"Hash\":\""+selected+"\",\"Size\":\"32\"}\n")};
        }
    }
}
