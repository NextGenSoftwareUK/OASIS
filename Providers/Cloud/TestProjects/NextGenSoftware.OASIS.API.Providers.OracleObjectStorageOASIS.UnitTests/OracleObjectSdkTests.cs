using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Oci.Common;
using Oci.Common.Auth;
using Oci.Common.Model;
using Oci.ObjectstorageService;

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class OracleObjectSdkTests
{
    private const string Endpoint="http://127.0.0.1:18661/";
    private static ObjectStorageClient Client()
    {
        using var rsa=RSA.Create(2048);
        var client=new ObjectStorageClient(new SimpleAuthenticationDetailsProvider {
            TenantId="ocid1.tenancy.oc1..synthetic",UserId="ocid1.user.oc1..synthetic",Fingerprint="aa:bb:cc",
            PrivateKeySupplier=new PrivateKeySupplier(rsa.ExportRSAPrivateKeyPem()),Region=Region.US_ASHBURN_1
        },new ClientConfiguration{TimeoutMillis=5000},Endpoint);
        client.SetEndpoint(Endpoint);
        client.SetRealmSpecificEndpointTemplate(Endpoint);
        var actual=client.GetEndpoint();
        Assert.AreEqual("127.0.0.1",actual.Host);Assert.AreEqual(18661,actual.Port);Assert.AreEqual("http",actual.Scheme);
        return client;
    }
    private static async Task Reply(HttpListenerContext context,int status,string body)
    {
        context.Response.StatusCode=status;
        context.Response.ContentType="application/json";
        var bytes=Encoding.UTF8.GetBytes(body);context.Response.ContentLength64=bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
    }
    private static async Task Exchange(Func<OracleObjectStorageBackend,Task> action,params Func<HttpListenerContext,Task>[] responses)
    {
        using var listener=new HttpListener();listener.Prefixes.Add(Endpoint);listener.Start();
        using var client=Client();
        var server=Task.Run(async()=>{
            foreach(var response in responses) {
                var context=await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(10));
                try {
                    StringAssert.StartsWith(context.Request.Headers["Authorization"],"Signature ");
                    StringAssert.StartsWith(context.Request.RawUrl,"/n/ns/b/bucket");
                    await response(context);
                }finally{context.Response.Close();}
            }
        });
        try{await Task.WhenAll(action(new(client,"ns","bucket")),server);}
        catch(OciException cause){Console.WriteLine("Observed SDK request endpoint: "+cause.ApiDetails?.RequestEndpoint);throw;}
        finally{listener.Stop();}
    }

    [DataTestMethod][DataRow("get")][DataRow("put")][DataRow("delete")][DataRow("verify")]
    public async Task NativeSdkDispatchesAuthenticatedOperations(string operation)
    {
        await Exchange(async backend=>{
            if(operation=="get")Assert.AreEqual("tree 🌳",await backend.GetAsync("oasis/record"));
            else if(operation=="put")await backend.PutAsync("oasis/record","tree 🌳");
            else if(operation=="delete")await backend.DeleteAsync("oasis/record");
            else await backend.VerifyAsync();
        },async context=>{
            Assert.AreEqual(operation switch{"get"=>"GET","put"=>"PUT","delete"=>"DELETE",_=>"HEAD"},context.Request.HttpMethod);
            if(operation=="put"){
                using var reader=new StreamReader(context.Request.InputStream);
                Assert.AreEqual("tree 🌳",await reader.ReadToEndAsync());
                Assert.AreEqual(Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes("tree 🌳"))),context.Request.Headers["Content-MD5"]);
            }
            await Reply(context,operation=="delete"?204:200,operation=="get"?"tree 🌳":"");
        });
    }

    [TestMethod]
    public async Task NativeListingFollowsEveryCursorWithoutDroppingPrefix()
    {
        await Exchange(async backend=>CollectionAssert.AreEqual(new[]{"oasis/a","oasis/b"},(await backend.ListKeysAsync("oasis/")).ToArray()),
            async context=>{Assert.AreEqual("oasis/",context.Request.QueryString["prefix"]);Assert.IsNull(context.Request.QueryString["start"]);await Reply(context,200,"{\"objects\":[{\"name\":\"oasis/a\"}],\"nextStartWith\":\"cursor-b\"}");},
            async context=>{Assert.AreEqual("oasis/",context.Request.QueryString["prefix"]);Assert.AreEqual("cursor-b",context.Request.QueryString["start"]);await Reply(context,200,"{\"objects\":[{\"name\":\"oasis/b\"}]}");});
    }

    [DataTestMethod][DataRow("ObjectNotFound")][DataRow("NotAuthorizedOrNotFound")]
    public async Task OnlyExplicitObjectAbsenceBecomesMissing(string code)
    {
        await Exchange(async backend=>{
            if(code=="ObjectNotFound")Assert.IsNull(await backend.GetAsync("oasis/missing"));
            else await Assert.ThrowsExceptionAsync<OciException>(()=>backend.GetAsync("oasis/missing"));
        },context=>Reply(context,404,"{\"code\":\""+code+"\",\"message\":\"controlled\"}"));
    }

    [DataTestMethod][DataRow("duplicate")][DataRow("outside")][DataRow("cursor")]
    public async Task CorruptListingCannotReturnPartialOrLoopForever(string corruption)
    {
        Func<HttpListenerContext,Task> first=context=>Reply(context,200,corruption switch {
            "duplicate"=>"{\"objects\":[{\"name\":\"oasis/a\"},{\"name\":\"oasis/a\"}]}",
            "outside"=>"{\"objects\":[{\"name\":\"foreign/a\"}]}",
            _=>"{\"objects\":[],\"nextStartWith\":\"same-cursor\"}"});
        var responses=corruption=="cursor"?new[]{first,first}:new[]{first};
        await Exchange(backend=>Assert.ThrowsExceptionAsync<InvalidDataException>(()=>backend.ListKeysAsync("oasis/")),responses);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task UncertainMutationsAreNotRetriedAndRequireReconciliation(bool delete)
    {
        await Exchange(async backend=>{
            var cause=await Assert.ThrowsExceptionAsync<OracleObjectMutationReconciliationException>(
                ()=>delete?backend.DeleteAsync("oasis/uncertain"):backend.PutAsync("oasis/uncertain","owned"));
            Assert.AreEqual("oasis/uncertain",cause.ObjectName);Assert.IsInstanceOfType<OciException>(cause.InnerException);
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(cause));
        },context=>Reply(context,503,"{\"code\":\"ServiceUnavailable\",\"message\":\"controlled\"}"));
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ConditionalWritesUseNativeHeadersAndTypedKnownConflict(bool create)
    {
        await Exchange(async backend=>{
            var conflict=await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(
                ()=>create?backend.CreateOnlyAsync("oasis/conditional","owned"):backend.ReplaceUnchangedAsync("oasis/conditional","owned","observed-etag"));
            Assert.IsInstanceOfType<OciException>(conflict.Data["NativeCause"]);Assert.IsFalse(NativeMutationReconciliation.IsRequired(conflict));
        },async context=>{
            Assert.AreEqual(create?"*":null,context.Request.Headers["If-None-Match"]);
            Assert.AreEqual(create?null:"observed-etag",context.Request.Headers["If-Match"]);
            await Reply(context,412,"{\"code\":\"PreconditionFailed\",\"message\":\"controlled\"}");
        });
    }

    [TestMethod]
    public async Task PreCancelledRequestsDoNotDispatch()
    {
        using var client=Client();var backend=new OracleObjectStorageBackend(client,"ns","bucket");
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>backend.PutAsync("key","value",cancellation.Token));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>backend.GetAsync("key",cancellation.Token));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>backend.DeleteAsync("key",cancellation.Token));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>backend.ListKeysAsync("",cancellation.Token));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>backend.VerifyAsync(cancellation.Token));
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task PublicRecordsUseSharedQueriesAndHistoryWithoutPublishingPrivateKeysOrValues(bool synchronous)
    {
        using var listener=new HttpListener();listener.Prefixes.Add(Endpoint);listener.Start();
        using var client=Client();using var stopped=new CancellationTokenSource();
        var objects=new System.Collections.Concurrent.ConcurrentDictionary<string,string>(StringComparer.Ordinal);
        var server=Task.Run(async()=>{
            while(!stopped.IsCancellationRequested) {
                HttpListenerContext context;
                try{context=await listener.GetContextAsync().WaitAsync(stopped.Token);}
                catch(Exception cause)when(stopped.IsCancellationRequested&&(cause is OperationCanceledException||cause is HttpListenerException||cause is ObjectDisposedException)){return;}
                try {
                    StringAssert.StartsWith(context.Request.Headers["Authorization"],"Signature ");
                    var path=context.Request.Url!.AbsolutePath;const string objectPath="/n/ns/b/bucket/o/";
                    if(context.Request.HttpMethod=="HEAD"){await Reply(context,200,"");continue;}
                    if(path=="/n/ns/b/bucket/o") {
                        Assert.AreEqual("GET",context.Request.HttpMethod);var prefix=context.Request.QueryString["prefix"]??"";
                        await Reply(context,200,System.Text.Json.JsonSerializer.Serialize(new{objects=objects.Keys.Where(key=>key.StartsWith(prefix,StringComparison.Ordinal)).Select(key=>new{name=key}).ToArray()}));continue;
                    }
                    StringAssert.StartsWith(path,objectPath);var key=Uri.UnescapeDataString(path[objectPath.Length..]);
                    StringAssert.Matches(key,new System.Text.RegularExpressions.Regex("^(?:oasis|isolated)/oasis/private/v1/[a-f0-9]{64}$"));
                    if(context.Request.HttpMethod=="PUT") {
                        using var reader=new StreamReader(context.Request.InputStream);objects[key]=await reader.ReadToEndAsync();await Reply(context,200,"");
                    }else if(context.Request.HttpMethod=="GET") {
                        if(objects.TryGetValue(key,out var value))await Reply(context,200,value);
                        else await Reply(context,404,"{\"code\":\"ObjectNotFound\",\"message\":\"controlled\"}");
                    }else {Assert.AreEqual("DELETE",context.Request.HttpMethod);objects.TryRemove(key,out _);await Reply(context,204,"");}
                }finally{context.Response.Close();}
            }
        });
        var storageKey=RandomNumberGenerator.GetBytes(32);
        try {
            using var provider=new OracleObjectStorageOASIS(client,"ns","bucket",()=>storageKey.ToArray()){IsVersionControlEnabled=true};
            var activated=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);
            var avatar=new NextGenSoftware.OASIS.API.Core.Holons.Avatar{Id=Guid.NewGuid(),Username="private-user",Email="private-user@example.invalid",IsActive=true};
            var saved=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Message);Assert.AreEqual(1,avatar.Version);
            avatar.Description="private-update";
            var updated=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);Assert.IsFalse(updated.IsError,updated.Message);Assert.AreEqual(2,avatar.Version);
            var byId=synchronous?provider.LoadAvatar(avatar.Id):await provider.LoadAvatarAsync(avatar.Id);
            var byName=synchronous?provider.LoadAvatarByUsername(avatar.Username):await provider.LoadAvatarByUsernameAsync(avatar.Username);
            var byEmail=synchronous?provider.LoadAvatarByEmail(avatar.Email):await provider.LoadAvatarByEmailAsync(avatar.Email);
            foreach(var found in new[]{byId,byName,byEmail}){Assert.IsFalse(found.IsError,found.Message);Assert.AreEqual(avatar.Id,found.Result.Id);Assert.AreEqual(2,found.Result.Version);}
            var historical=synchronous?provider.LoadAvatar(avatar.Id,1):await provider.LoadAvatarAsync(avatar.Id,1);Assert.IsFalse(historical.IsError,historical.Message);Assert.AreEqual(1,historical.Result.Version);Assert.IsNull(historical.Result.Description);
            var isolatedKey=RandomNumberGenerator.GetBytes(32);
            try {
                using var isolated=new OracleObjectStorageOASIS(client,"ns","bucket",()=>isolatedKey.ToArray(),keyPrefix:"isolated/");
                var separate=await isolated.ActivateProviderAsync();Assert.IsFalse(separate.IsError,separate.Message);
                Assert.IsTrue((await isolated.LoadAvatarByEmailAsync(avatar.Email)).IsError);
                var another=new NextGenSoftware.OASIS.API.Core.Holons.Avatar{Id=Guid.NewGuid(),Username=avatar.Username,Email=avatar.Email,IsActive=true};
                var anotherSaved=await isolated.SaveAvatarAsync(another);Assert.IsFalse(anotherSaved.IsError,anotherSaved.Message);
                Assert.AreEqual(another.Id,(await isolated.LoadAvatarByEmailAsync(avatar.Email)).Result.Id);
                Assert.AreEqual(avatar.Id,(await provider.LoadAvatarByEmailAsync(avatar.Email)).Result.Id);
            }finally{CryptographicOperations.ZeroMemory(isolatedKey);}
            var holon=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=Guid.NewGuid(),Name="private-holon",CreatedByAvatarId=avatar.Id,MetaData=new(){["tag"]="private-tag"}};
            var stored=synchronous?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(stored.IsError,stored.Message);
            var selected=synchronous?provider.LoadHolonsByMetaData("tag","private-tag",loadChildren:false):await provider.LoadHolonsByMetaDataAsync("tag","private-tag",loadChildren:false);
            Assert.IsFalse(selected.IsError,selected.Message);Assert.AreEqual(holon.Id,selected.Result.Single().Id);
            Assert.IsTrue(objects.Count>0);
            foreach(var value in objects.Values)foreach(var marker in new[]{avatar.Username,avatar.Email,avatar.Id.ToString(),holon.Name,"private-tag"})Assert.IsFalse(value.Contains(marker,StringComparison.Ordinal));
            var removed=synchronous?provider.DeleteAvatar(avatar.Id,softDelete:false):await provider.DeleteAvatarAsync(avatar.Id,softDelete:false);Assert.IsFalse(removed.IsError,removed.Message);
            Assert.IsTrue((await provider.LoadAvatarAsync(avatar.Id)).IsError);
            provider.Dispose();Assert.IsFalse(provider.IsProviderActivated);
            Assert.IsTrue((await provider.ActivateProviderAsync()).IsError);
            await new OracleObjectStorageBackend(client,"ns","bucket").VerifyAsync(); // borrowed client remains caller-owned
        }finally{CryptographicOperations.ZeroMemory(storageKey);stopped.Cancel();listener.Stop();await server;}
    }

    [TestMethod]
    public async Task MissingPersistedEncryptionKeyFailsBeforeAnySdkRequest()
    {
        using var client=Client();
        using var provider=new OracleObjectStorageOASIS(client,"ns","bucket",()=>throw new InvalidOperationException("owned missing storage key"));
        var result=await provider.ActivateProviderAsync();Assert.IsTrue(result.IsError);Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsInstanceOfType<InvalidOperationException>(result.Exception);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ArchivedCredentialsCannotLoadKeysOrDispatchSdkRequests(bool synchronous)
    {
        using var client=Client();var keyReads=0;
        using var provider=new OracleObjectStorageOASIS(client,"ns","bucket",()=>{keyReads++;throw new InvalidOperationException("must not read storage key");});
        foreach(var kind in new[]{0,1,2,3,4}) {
            var result=kind switch {
                0=>synchronous?provider.LoadAvatarByVerificationToken("owned",1):await provider.LoadAvatarByVerificationTokenAsync("owned",1),
                1=>synchronous?provider.LoadAvatarByResetToken("owned",1):await provider.LoadAvatarByResetTokenAsync("owned",1),
                2=>synchronous?provider.LoadAvatarByRefreshToken("owned",1):await provider.LoadAvatarByRefreshTokenAsync("owned",1),
                3=>synchronous?provider.LoadAvatarByPublicKey("owned",1):await provider.LoadAvatarByPublicKeyAsync("owned",1),
                _=>synchronous?provider.LoadAvatarByPrivateKey("owned",1):await provider.LoadAvatarByPrivateKeyAsync("owned",1)};
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
            Assert.AreEqual(0,keyReads);
        }
    }
}
