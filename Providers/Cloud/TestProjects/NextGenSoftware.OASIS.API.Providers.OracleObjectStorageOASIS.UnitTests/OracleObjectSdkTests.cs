using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Oci.Common;
using Oci.Common.Auth;
using Oci.Common.Model;
using Oci.ObjectstorageService;

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class OracleObjectSdkTests
{
    private sealed class NamedOwner:NextGenSoftware.OASIS.API.Core.Holons.Holon
    {
        public List<NextGenSoftware.OASIS.API.Core.Holons.Holon> Components {get;set;}=new();
        public override System.Collections.ObjectModel.ReadOnlyCollection<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> AllChildren
            =>Children.Concat<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<NextGenSoftware.OASIS.API.Core.Holons.HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new NextGenSoftware.OASIS.API.Core.Holons.HolonChildCollection(nameof(Components),Components,rows=>Components=rows.Cast<NextGenSoftware.OASIS.API.Core.Holons.Holon>().ToList())}).ToArray();
    }
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
    private static async Task Reply(HttpListenerContext context,int status,string body,bool acknowledgePut=true)
    {
        context.Response.StatusCode=status;
        context.Response.ContentType="application/json";
        // Controlled service acknowledgement; the primitive test independently checks body/hash.
        if(acknowledgePut&&status==200&&context.Request.HttpMethod=="PUT"){
            context.Response.Headers["ETag"]="\"controlled-opaque-revision\"";
            context.Response.Headers["opc-content-md5"]=context.Request.Headers["Content-MD5"];
        }
        var bytes=Encoding.UTF8.GetBytes(body);context.Response.ContentLength64=bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
    }
    private static async Task Exchange(Func<OracleObjectStorageBackend,Task> action,params Func<HttpListenerContext,Task>[] responses)
        =>await ExchangeClient(client=>action(new(client,"ns","bucket")),responses);

    private static async Task ExchangeClient(Func<ObjectStorageClient,Task> action,params Func<HttpListenerContext,Task>[] responses)
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
        try{await Task.WhenAll(action(client),server);}
        catch(OciException cause){Console.WriteLine("Observed SDK request endpoint: "+cause.ApiDetails?.RequestEndpoint);throw;}
        finally{listener.Stop();}
    }

    [DataTestMethod][DataRow("success")][DataRow("create")][DataRow("replace")][DataRow("delete")][DataRow("occupied")]
    public async Task LiveTransportDriverKeepsOriginalUncertainJournalWithoutCleanupOrReplay(string outcome)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var work=Path.Combine(root.FullName,"TestResults","OracleObjectCurrent","LiveDriverControlled");
        Directory.CreateDirectory(work); // One fixed reusable evidence scope, not per-run directories.
        var pending=Path.Combine(work,"oci-live-transport.pending.json");var report=Path.Combine(work,"oci-live-transport.current.json");
        Assert.IsFalse(File.Exists(pending)||File.Exists(report),"Review original controlled fixture before reuse.");
        string? value=outcome=="occupied"?"foreign object":null;var revision="\"v1\"";var requests=0;var deletes=0;var puts=0;
        var expected=outcome switch{"occupied"=>2,"create"=>3,"replace"=>5,"delete"=>9,_=>10};
        async Task Service(HttpListenerContext context)
        {
            requests++;var method=context.Request.HttpMethod;
            if(method=="HEAD"){await Reply(context,200,"");return;}
            StringAssert.EndsWith(Uri.UnescapeDataString(context.Request.Url!.AbsolutePath),"/o/oasis-acceptance/transport-probe");
            if(method=="GET"){
                if(value==null)await Reply(context,404,"{\"code\":\"ObjectNotFound\",\"message\":\"controlled\"}");
                else{context.Response.Headers["ETag"]=revision;await Reply(context,200,value);}return;
            }
            if(method=="PUT"){
                puts++;using var reader=new StreamReader(context.Request.InputStream,Encoding.UTF8);var proposed=await reader.ReadToEndAsync();
                if(puts==1){Assert.AreEqual("*",context.Request.Headers["If-None-Match"]);value=proposed;}
                else if(context.Request.Headers["If-Match"]!=revision){await Reply(context,412,"{\"code\":\"PreconditionFailed\",\"message\":\"controlled\"}");return;}
                else{value=proposed;revision="\"v2\"";}
                if(outcome==(puts==1?"create":"replace")){await Reply(context,503,"{\"code\":\"ServiceUnavailable\",\"message\":\"after owned mutation\"}");return;}
                context.Response.Headers["ETag"]=revision;context.Response.Headers["opc-content-md5"]=context.Request.Headers["Content-MD5"];
                await Reply(context,200,"",acknowledgePut:false);return;
            }
            Assert.AreEqual("DELETE",method);deletes++;Assert.AreEqual(revision,context.Request.Headers["If-Match"]);value=null;
            await Reply(context,outcome=="delete"?503:204,outcome=="delete"?"{\"code\":\"ServiceUnavailable\",\"message\":\"after owned delete\"}":"");
        }
        await ExchangeClient(async client=>{
            if(outcome=="success")await OracleLiveTransportAcceptance.RunAsync(client,"ns","bucket","oasis-acceptance/",work);
            else if(outcome=="occupied")await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>OracleLiveTransportAcceptance.RunAsync(client,"ns","bucket","oasis-acceptance/",work));
            else await Assert.ThrowsExceptionAsync<OracleObjectMutationReconciliationException>(()=>OracleLiveTransportAcceptance.RunAsync(client,"ns","bucket","oasis-acceptance/",work));
        },Enumerable.Repeat<Func<HttpListenerContext,Task>>(Service,expected).ToArray());
        Assert.AreEqual(expected,requests);Assert.AreEqual(outcome is "success" or "delete"?1:0,deletes);
        if(outcome=="occupied"){Assert.AreEqual(0,puts);Assert.AreEqual("foreign object",value);Assert.IsFalse(File.Exists(pending)||File.Exists(report));return;}
        var evidence=outcome=="success"?report:pending;
        using(var document=System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(evidence))){
            var state=document.RootElement;Assert.AreEqual("ns",state.GetProperty("namespaceName").GetString());Assert.AreEqual("bucket",state.GetProperty("bucket").GetString());
            Assert.AreEqual("oasis-acceptance/transport-probe",state.GetProperty("objectName").GetString());
            Assert.AreEqual(outcome switch{"create"=>"before-create","replace"=>"before-replace","delete"=>"before-conditional-delete",_=>"verified"},state.GetProperty("state").GetString());
            if(outcome=="success"){Assert.IsFalse(state.GetProperty("fullProviderVerified").GetBoolean());Assert.AreEqual(10,state.GetProperty("logicalSdkCalls").GetInt32());}
        }
        Assert.AreEqual(outcome!="success",File.Exists(pending));Assert.AreEqual(outcome=="success",File.Exists(report));
        File.Delete(evidence); // Verified synthetic LOOPBACK fixture only; real runner never auto-cleans errors.
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
            if(operation=="get")context.Response.Headers["ETag"]="\"observed-read\"";
            await Reply(context,operation=="delete"?204:200,operation=="get"?"tree 🌳":"");
        });
    }

    [DataTestMethod]
    [DataRow("put","missing-md5")][DataRow("create","missing-md5")][DataRow("replace","missing-md5")]
    [DataRow("put","malformed-md5")][DataRow("create","malformed-md5")][DataRow("replace","malformed-md5")]
    [DataRow("put","mismatched-md5")][DataRow("create","mismatched-md5")][DataRow("replace","mismatched-md5")]
    [DataRow("put","missing-etag")][DataRow("create","missing-etag")][DataRow("replace","missing-etag")]
    [DataRow("put","blank-etag")][DataRow("create","blank-etag")][DataRow("replace","blank-etag")]
    [DataRow("put","valid")][DataRow("create","valid")][DataRow("replace","valid")]
    public async Task UploadAcknowledgementRequiresMatchingServerDigestAndOpaqueRevision(string operation,string acknowledgement)
    {
        await Exchange(async backend=>{
            Task Write()=>operation switch {
                "create"=>backend.CreateOnlyAsync("oasis/upload-ack","tree 🌳"),
                "replace"=>backend.ReplaceUnchangedAsync("oasis/upload-ack","tree 🌳","previous-revision"),
                _=>backend.PutAsync("oasis/upload-ack","tree 🌳")
            };
            if(acknowledgement=="valid"){await Write();return;}
            var cause=await Assert.ThrowsExceptionAsync<OracleObjectMutationReconciliationException>(Write);
            Assert.AreEqual("oasis/upload-ack",cause.ObjectName);
            Assert.IsInstanceOfType<InvalidDataException>(cause.InnerException);
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(cause));
        },async context=>{
            Assert.AreEqual("PUT",context.Request.HttpMethod);
            Assert.AreEqual(operation=="create"?"*":null,context.Request.Headers["If-None-Match"]);
            Assert.AreEqual(operation=="replace"?"previous-revision":null,context.Request.Headers["If-Match"]);
            using var reader=new StreamReader(context.Request.InputStream);var body=await reader.ReadToEndAsync();
            Assert.AreEqual("tree 🌳",body);
            var digest=Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes(body)));
            Assert.AreEqual(digest,context.Request.Headers["Content-MD5"]);
            if(acknowledgement!="missing-etag")context.Response.Headers["ETag"]=acknowledgement=="blank-etag"?"   ":"\"opaque-not-an-md5-or-uuid\"";
            if(acknowledgement!="missing-md5")context.Response.Headers["opc-content-md5"]=acknowledgement switch {
                "malformed-md5"=>"not-base64",
                "mismatched-md5"=>Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes("different-body"))),
                _=>digest
            };
            await Reply(context,200,"",acknowledgePut:false);
        });
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task PublicSaveDoesNotPublishProposalAfterMalformedSuccessfulUpload(bool synchronous)
    {
        await Exchange(async _=>{
            using var client=Client();var key=RandomNumberGenerator.GetBytes(32);
            try{
                using var provider=new OracleObjectStorageOASIS(client,"ns","bucket",()=>key.ToArray());
                var caller=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Name="private-unacknowledged-proposal",Version=9};
                var originalKeys=caller.ProviderUniqueStorageKey;
                var result=synchronous?provider.SaveHolon(caller,saveChildren:false):await provider.SaveHolonAsync(caller,saveChildren:false);
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
                Assert.AreEqual(Guid.Empty,caller.Id);Assert.AreEqual(9,caller.Version);Assert.AreSame(originalKeys,caller.ProviderUniqueStorageKey);
                Assert.IsInstanceOfType<OracleObjectMutationReconciliationException>(result.Exception);
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
            }finally{CryptographicOperations.ZeroMemory(key);}
        },context=>Reply(context,404,"{\"code\":\"ObjectNotFound\",\"message\":\"controlled\"}"),async context=>{
            Assert.AreEqual("PUT",context.Request.HttpMethod);Assert.AreEqual("*",context.Request.Headers["If-None-Match"]);
            using var reader=new StreamReader(context.Request.InputStream);
            Assert.IsFalse((await reader.ReadToEndAsync()).Contains("private-unacknowledged-proposal",StringComparison.Ordinal));
            context.Response.Headers["ETag"]="\"opaque-revision\"";
            context.Response.Headers["opc-content-md5"]="wrong-checksum";
            await Reply(context,200,"",acknowledgePut:false);
        });
    }

    [TestMethod]
    public async Task NativeListingFollowsEveryCursorWithoutDroppingPrefix()
    {
        await Exchange(async backend=>CollectionAssert.AreEqual(new[]{"oasis/a","oasis/b"},(await backend.ListKeysAsync("oasis/")).ToArray()),
            async context=>{Assert.AreEqual("oasis/",context.Request.QueryString["prefix"]);Assert.IsNull(context.Request.QueryString["start"]);await Reply(context,200,"{\"objects\":[{\"name\":\"oasis/a\"}],\"nextStartWith\":\"cursor-b\"}");},
            async context=>{Assert.AreEqual("oasis/",context.Request.QueryString["prefix"]);Assert.AreEqual("cursor-b",context.Request.QueryString["start"]);await Reply(context,200,"{\"objects\":[{\"name\":\"oasis/b\"}]}");});
    }

    [TestMethod]
    public async Task NativeSnapshotReturnsValueAndOpaqueEtagFromOneGet()
    {
        await Exchange(async backend=>{
            NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IConditionalKeyValueBackend conditional=backend;
            var snapshot=await conditional.ReadObservedAsync("oasis/snapshot");Assert.IsNotNull(snapshot);
            Assert.AreEqual("owned-value",snapshot.Value);Assert.AreEqual("\"native-etag-not-a-guid\"",snapshot.Revision);
        },async context=>{
            Assert.AreEqual("GET",context.Request.HttpMethod);context.Response.Headers["ETag"]="\"native-etag-not-a-guid\"";
            await Reply(context,200,"owned-value");
        });
    }

    [TestMethod]
    public async Task NativeSnapshotWithoutEtagIsCorruptionNotAnUnconditionalObservation()
    {
        await Exchange(async backend=>{
            var cause=await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>backend.ReadObservedAsync("oasis/snapshot"));
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(cause));
        },context=>Reply(context,200,"owned-value"));
    }

    [DataTestMethod][DataRow(204)][DataRow(412)][DataRow(404)][DataRow(403)][DataRow(503)]
    public async Task ConditionalDeleteUsesObservedEtagAndPreservesOutcomeClassification(int status)
    {
        await Exchange(async backend=>{
            NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IConditionalKeyValueBackend conditional=backend;
            Task Delete()=>conditional.DeleteUnchangedAsync("oasis/conditional-delete","\"observed-delete\"");
            if(status==204)await Delete();
            else if(status==412||status==404){
                var conflict=await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(Delete);
                Assert.IsInstanceOfType<OciException>(conflict.Data["NativeCause"]);Assert.IsFalse(NativeMutationReconciliation.IsRequired(conflict));
            }else if(status==403){
                var rejected=await Assert.ThrowsExceptionAsync<OciException>(Delete);Assert.IsFalse(NativeMutationReconciliation.IsRequired(rejected));
            }else{
                var uncertain=await Assert.ThrowsExceptionAsync<OracleObjectMutationReconciliationException>(Delete);
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(uncertain));Assert.AreEqual("oasis/conditional-delete",uncertain.ObjectName);
            }
        },async context=>{
            Assert.AreEqual("DELETE",context.Request.HttpMethod);Assert.AreEqual("\"observed-delete\"",context.Request.Headers["If-Match"]);
            await Reply(context,status,status==204?"":"{\"code\":\""+(status==404?"ObjectNotFound":"controlled")+"\",\"message\":\"controlled\"}");
        });
    }

    [DataTestMethod][DataRow("ObjectNotFound")][DataRow("NotAuthorizedOrNotFound")]
    public async Task OnlyExplicitObjectAbsenceBecomesMissing(string code)
    {
        await Exchange(async backend=>{
            if(code=="ObjectNotFound")Assert.IsNull(await backend.ReadObservedAsync("oasis/missing"));
            else await Assert.ThrowsExceptionAsync<OciException>(()=>backend.ReadObservedAsync("oasis/missing"));
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
            NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IConditionalKeyValueBackend conditional=backend;
            var conflict=await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(
                ()=>create?conditional.CreateOnlyAsync("oasis/conditional","owned"):conditional.ReplaceUnchangedAsync("oasis/conditional","owned","observed-etag"));
            Assert.IsInstanceOfType<OciException>(conflict.Data["NativeCause"]);Assert.IsFalse(NativeMutationReconciliation.IsRequired(conflict));
        },async context=>{
            Assert.AreEqual(create?"*":null,context.Request.Headers["If-None-Match"]);
            Assert.AreEqual(create?null:"observed-etag",context.Request.Headers["If-Match"]);
            await Reply(context,412,"{\"code\":\"PreconditionFailed\",\"message\":\"controlled\"}");
        });
    }

    [DataTestMethod][DataRow("ObjectNotFound")][DataRow("NotAuthorizedOrNotFound")]
    public async Task ConditionalReplacementDistinguishesDisappearedObjectFromHiddenBucket(string code)
    {
        await Exchange(async backend=>{
            Task Replace()=>backend.ReplaceUnchangedAsync("oasis/disappeared","owned","observed-etag");
            if(code=="ObjectNotFound"){
                var conflict=await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(Replace);
                Assert.IsInstanceOfType<OciException>(conflict.Data["NativeCause"]);
                Assert.IsFalse(NativeMutationReconciliation.IsRequired(conflict));
            }else await Assert.ThrowsExceptionAsync<OciException>(Replace);
        },async context=>{
            Assert.AreEqual("PUT",context.Request.HttpMethod);
            Assert.AreEqual("observed-etag",context.Request.Headers["If-Match"]);
            await Reply(context,404,"{\"code\":\""+code+"\",\"message\":\"controlled\"}");
        });
    }

    [TestMethod]
    public async Task PreCancelledRequestsDoNotDispatch()
    {
        using var client=Client();var backend=new OracleObjectStorageBackend(client,"ns","bucket");
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>backend.PutAsync("key","value",cancellation.Token));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>backend.GetAsync("key",cancellation.Token));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>backend.ReadObservedAsync("key",cancellation.Token));
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
        var conditionalCreates=0;var conditionalReplacements=0;var conditionalDeletes=0;
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
                        if(context.Request.Headers["If-None-Match"]=="*"){
                            conditionalCreates++;if(objects.ContainsKey(key)){await Reply(context,412,"{\"code\":\"PreconditionFailed\",\"message\":\"controlled\"}");continue;}
                        }
                        if(context.Request.Headers["If-Match"] is string expected){
                            conditionalReplacements++;
                            if(!objects.TryGetValue(key,out var current)||"\""+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(current)))+"\""!=expected){await Reply(context,412,"{\"code\":\"PreconditionFailed\",\"message\":\"controlled\"}");continue;}
                        }
                        using var reader=new StreamReader(context.Request.InputStream);objects[key]=await reader.ReadToEndAsync();await Reply(context,200,"");
                    }else if(context.Request.HttpMethod=="GET") {
                        if(objects.TryGetValue(key,out var value)){context.Response.Headers["ETag"]="\""+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))+"\"";await Reply(context,200,value);}
                        else await Reply(context,404,"{\"code\":\"ObjectNotFound\",\"message\":\"controlled\"}");
                    }else {
                        Assert.AreEqual("DELETE",context.Request.HttpMethod);
                        if(context.Request.Headers["If-Match"] is string expected){
                            conditionalDeletes++;
                            if(!objects.TryGetValue(key,out var current)||"\""+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(current)))+"\""!=expected){await Reply(context,412,"{\"code\":\"PreconditionFailed\",\"message\":\"controlled\"}");continue;}
                        }
                        objects.TryRemove(key,out _);await Reply(context,204,"");
                    }
                }finally{context.Response.Close();}
            }
        });
        var storageKey=RandomNumberGenerator.GetBytes(32);
        try {
            using var provider=new OracleObjectStorageOASIS(client,"ns","bucket",()=>storageKey.ToArray()){IsVersionControlEnabled=true};
            var activated=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);
            var avatar=new NextGenSoftware.OASIS.API.Core.Holons.Avatar{Id=Guid.NewGuid(),Username="private-user",Email="private-user@example.invalid",IsActive=true};
            var saved=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Message);Assert.AreEqual(1,avatar.Version);
            Assert.IsTrue(saved.IsSaved);Assert.AreEqual(1,saved.SavedCount);
            avatar.Description="private-update";
            var updated=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);Assert.IsFalse(updated.IsError,updated.Message);Assert.AreEqual(2,avatar.Version);
            Assert.IsTrue(updated.IsSaved);Assert.AreEqual(1,updated.SavedCount);
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
            var initialCreates=conditionalCreates;var initialReplacements=conditionalReplacements;var initialDeletes=conditionalDeletes;
            var detail=new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail{Id=avatar.Id,Karma=111,XP=222};
            var detailCreated=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(detailCreated.IsError,detailCreated.Message);
            Assert.IsTrue(detailCreated.IsSaved);Assert.AreEqual(1,detailCreated.SavedCount);
            detail.Karma=333;detail.XP=444;
            var detailUpdated=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(detailUpdated.IsError,detailUpdated.Message);
            Assert.IsTrue(detailUpdated.IsSaved);Assert.AreEqual(1,detailUpdated.SavedCount);
            Assert.AreEqual(2,detail.Version);
            var previousDetail=await provider.LoadAvatarDetailAsync(avatar.Id,version:1);Assert.IsFalse(previousDetail.IsError,previousDetail.Message);
            Assert.AreEqual(111L,previousDetail.Result.Karma);Assert.AreEqual(222L,previousDetail.Result.XP);
            var currentDetail=await provider.LoadAvatarDetailAsync(avatar.Id);Assert.IsFalse(currentDetail.IsError,currentDetail.Message);
            Assert.AreEqual(detail.Karma,currentDetail.Result.Karma);Assert.AreEqual(detail.XP,currentDetail.Result.XP);
            var originalParent=Guid.NewGuid();var nextParent=Guid.NewGuid();
            var holon=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=Guid.NewGuid(),ParentHolonId=originalParent,Name="private-holon",CreatedByAvatarId=avatar.Id,MetaData=new(){["tag"]="private-tag"}};
            var stored=synchronous?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(stored.IsError,stored.Message);
            Assert.IsTrue(stored.IsSaved);Assert.AreEqual(1,stored.SavedCount);
            holon.Name="private-updated-holon";holon.ParentHolonId=nextParent;
            var updatedHolon=synchronous?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(updatedHolon.IsError,updatedHolon.Message);
            Assert.IsTrue(updatedHolon.IsSaved);Assert.AreEqual(1,updatedHolon.SavedCount);
            Assert.AreEqual(2,holon.Version);Assert.AreEqual(6,conditionalCreates-initialCreates);Assert.AreEqual(2,conditionalReplacements-initialReplacements);Assert.AreEqual(1,conditionalDeletes-initialDeletes);
            Assert.IsFalse((await provider.LoadHolonsForParentAsync(originalParent,loadChildren:false)).Result.Any());
            Assert.AreEqual(holon.Id,(await provider.LoadHolonsForParentAsync(nextParent,loadChildren:false)).Result.Single().Id);
            Assert.AreEqual("private-holon",(await provider.LoadHolonAsync(holon.Id,loadChildren:false,version:1)).Result.Name);
            var selected=synchronous?provider.LoadHolonsByMetaData("tag","private-tag",loadChildren:false):await provider.LoadHolonsByMetaDataAsync("tag","private-tag",loadChildren:false);
            Assert.IsFalse(selected.IsError,selected.Message);Assert.AreEqual(holon.Id,selected.Result.Single().Id);
            Assert.IsTrue(objects.Count>0);
            foreach(var value in objects.Values)foreach(var marker in new[]{avatar.Username,avatar.Email,avatar.Id.ToString(),holon.Name,"private-tag"})Assert.IsFalse(value.Contains(marker,StringComparison.Ordinal));
            var beforePairCreates=conditionalCreates;var beforePairReplacements=conditionalReplacements;
            var softPair=synchronous?provider.DeleteAvatar(avatar.Id,softDelete:true):await provider.DeleteAvatarAsync(avatar.Id,softDelete:true);
            Assert.IsFalse(softPair.IsError,softPair.Exception?.ToString());Assert.IsTrue(softPair.IsDeleted);
            Assert.AreEqual(2,conditionalCreates-beforePairCreates);Assert.AreEqual(2,conditionalReplacements-beforePairReplacements);
            var softAvatar=(await provider.LoadAvatarAsync(avatar.Id)).Result;var softDetail=(await provider.LoadAvatarDetailAsync(avatar.Id)).Result;
            Assert.IsTrue(softAvatar.IsDeleted);Assert.IsTrue(softDetail.IsDeleted);Assert.AreEqual(softAvatar.DeletedDate,softDetail.DeletedDate);
            Assert.AreEqual(3,softAvatar.Version);Assert.AreEqual(3,softDetail.Version);
            Assert.IsFalse((await provider.LoadAvatarAsync(avatar.Id,version:2)).Result.IsDeleted);
            Assert.IsFalse((await provider.LoadAvatarDetailAsync(avatar.Id,version:2)).Result.IsDeleted);
            var beforeDetailDelete=conditionalDeletes;
            var deletedDetail=synchronous?provider.DeleteAvatarDetail(avatar.Id,softDelete:false):await provider.DeleteAvatarDetailAsync(avatar.Id,softDelete:false);
            Assert.IsFalse(deletedDetail.IsError,deletedDetail.Exception?.ToString());Assert.IsTrue(deletedDetail.IsDeleted);
            Assert.AreEqual(3,conditionalDeletes-beforeDetailDelete,"Current detail and both observed archives must use native If-Match deletion.");
            Assert.IsTrue((await provider.LoadAvatarDetailAsync(avatar.Id)).IsError);
            var beforeHolonDelete=conditionalDeletes;
            var deletedHolon=synchronous?provider.DeleteHolon(holon.Id):await provider.DeleteHolonAsync(holon.Id);
            Assert.IsFalse(deletedHolon.IsError,deletedHolon.Exception?.ToString());Assert.IsTrue(deletedHolon.IsDeleted);
            Assert.AreEqual(3,conditionalDeletes-beforeHolonDelete,"Current holon, parent link and history must use native If-Match deletion.");
            Assert.IsTrue((await provider.LoadHolonAsync(holon.Id,loadChildren:false)).IsError);
            var beforeAvatarDelete=conditionalDeletes;
            var removed=synchronous?provider.DeleteAvatar(avatar.Id,softDelete:false):await provider.DeleteAvatarAsync(avatar.Id,softDelete:false);Assert.IsFalse(removed.IsError,removed.Message);
            Assert.AreEqual(5,conditionalDeletes-beforeAvatarDelete,"Avatar, both identity indexes and both archives must use native If-Match deletion.");
            Assert.IsTrue((await provider.LoadAvatarAsync(avatar.Id)).IsError);
            var independent=new NextGenSoftware.OASIS.API.Core.Holons.Avatar{Id=Guid.NewGuid(),Username="private-independent-user",Email="independent@example.invalid",IsActive=true};
            Assert.IsFalse((await provider.SaveAvatarAsync(independent)).IsError);Assert.AreEqual(1,independent.Version);
            var independentDetail=new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail{Id=independent.Id,Karma=10,XP=20};
            for(var version=1;version<=3;version++){
                independentDetail.Karma=version*10;independentDetail.XP=version*20;
                Assert.IsFalse((await provider.SaveAvatarDetailAsync(independentDetail)).IsError);Assert.AreEqual(version,independentDetail.Version);
            }
            Assert.AreEqual(1,(await provider.LoadAvatarAsync(independent.Id)).Result.Version);
            var independentByName=synchronous?provider.LoadAvatarDetailByUsername(independent.Username,3):await provider.LoadAvatarDetailByUsernameAsync(independent.Username,3);
            var independentByEmail=synchronous?provider.LoadAvatarDetailByEmail(independent.Email,2):await provider.LoadAvatarDetailByEmailAsync(independent.Email,2);
            foreach(var observation in new[]{(Result:independentByName,Version:3),(Result:independentByEmail,Version:2)}){
                Assert.IsFalse(observation.Result.IsError,observation.Result.Message);Assert.AreEqual(independent.Id,observation.Result.Result.Id);
                Assert.AreEqual(observation.Version,observation.Result.Result.Version);Assert.AreEqual((long)observation.Version*10,observation.Result.Result.Karma);
            }
            Assert.IsFalse((await provider.DeleteAvatarDetailAsync(independent.Id,false)).IsError);
            Assert.IsFalse((await provider.DeleteAvatarAsync(independent.Id,false)).IsError);
            var owner=new NamedOwner{Id=Guid.NewGuid(),Name="private-named-owner"};
            var member=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=Guid.NewGuid(),Name="private-named-member"};owner.Components.Add(member);
            var manifest=NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Serialize(
                NextGenSoftware.OASIS.API.Core.Holons.HolonChildCollectionState.Capture(owner).CreateManifest(owner.Id,row=>row.Id));
            var named=synchronous?provider.SaveHolon(owner):await provider.SaveHolonAsync(owner);Assert.IsFalse(named.IsError,named.Exception?.ToString());
            Assert.AreEqual(manifest,owner.ChildRoleManifestJson);Assert.AreEqual(owner.Id,member.ParentHolonId);
            var currentOwner=(await provider.LoadHolonAsync(owner.Id,loadChildren:false)).Result;
            Assert.AreEqual(manifest,((NextGenSoftware.OASIS.API.Core.Holons.Holon)currentOwner).ChildRoleManifestJson);
            var memberBefore=OasisJson.Serialize((await provider.LoadHolonAsync(member.Id,loadChildren:false)).Result);
            var scalar=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=owner.Id,Name="private-named-scalar"};
            var rootOnly=synchronous?provider.SaveHolon(scalar,saveChildren:false):await provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(rootOnly.IsError,rootOnly.Exception?.ToString());
            Assert.AreEqual(manifest,scalar.ChildRoleManifestJson);
            Assert.AreEqual(memberBefore,OasisJson.Serialize((await provider.LoadHolonAsync(member.Id,loadChildren:false)).Result));
            var archivedOwner=(await provider.LoadHolonAsync(owner.Id,loadChildren:false,version:1)).Result;
            Assert.AreEqual(manifest,((NextGenSoftware.OASIS.API.Core.Holons.Holon)archivedOwner).ChildRoleManifestJson);
            owner.Components.Clear();var unlinked=synchronous?provider.SaveHolon(owner):await provider.SaveHolonAsync(owner);Assert.IsFalse(unlinked.IsError,unlinked.Exception?.ToString());
            var beforeRead=objects.OrderBy(pair=>pair.Key).ToArray();
            var noResurrection=synchronous?provider.LoadHolon(owner.Id):await provider.LoadHolonAsync(owner.Id);Assert.IsFalse(noResurrection.IsError,noResurrection.Message);Assert.AreEqual(0,noResurrection.Result.Children.Count);
            Assert.IsFalse((await provider.LoadHolonAsync(member.Id,loadChildren:false)).IsError,"Unlinking must not delete the independent member.");
            CollectionAssert.AreEqual(beforeRead,objects.OrderBy(pair=>pair.Key).ToArray(),"Reads must not repair/delete encrypted objects or indexes.");
            foreach(var value in objects.Values)foreach(var marker in new[]{owner.Name,member.Name,scalar.Name,owner.Id.ToString(),member.Id.ToString()})Assert.IsFalse(value.Contains(marker,StringComparison.Ordinal));
            Assert.IsFalse((await provider.DeleteHolonAsync(member.Id)).IsError);Assert.IsFalse((await provider.DeleteHolonAsync(owner.Id)).IsError);
            var importRoot=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=Guid.NewGuid(),Name="private-import-root"};
            var importChild=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=Guid.NewGuid(),Name="private-import-child"};
            importRoot.Children.Add(importChild);
            var imported=synchronous?provider.Import(new[]{importRoot}):await provider.ImportAsync(new[]{importRoot});
            Assert.IsFalse(imported.IsError,imported.Exception?.ToString());Assert.IsTrue(imported.Result);
            Assert.IsTrue(imported.IsSaved);Assert.AreEqual(2,imported.SavedCount);
            var importedGraph=await provider.LoadHolonAsync(importRoot.Id);
            Assert.IsFalse(importedGraph.IsError,importedGraph.Message);Assert.AreEqual(importChild.Id,importedGraph.Result.Children.Single().Id);
            foreach(var value in objects.Values)foreach(var marker in new[]{importRoot.Name,importChild.Name,importRoot.Id.ToString(),importChild.Id.ToString()})Assert.IsFalse(value.Contains(marker,StringComparison.Ordinal));
            Assert.IsTrue((await provider.LoadHolonAsync(member.Id,loadChildren:false)).IsError);Assert.IsTrue((await provider.LoadHolonAsync(owner.Id,loadChildren:false)).IsError);
            provider.Dispose();Assert.IsFalse(provider.IsProviderActivated);
            Assert.IsTrue((await provider.ActivateProviderAsync()).IsError);
            await new OracleObjectStorageBackend(client,"ns","bucket").VerifyAsync(); // borrowed client remains caller-owned
        }finally{CryptographicOperations.ZeroMemory(storageKey);stopped.Cancel();listener.Stop();await server;}
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task PublicSavePreservesNativeUncertaintyAndCallerUntilAllWritesAcknowledge(bool synchronous,bool partial)
    {
        var puts=0;
        Func<HttpListenerContext,Task> missing=context=>{
            Assert.AreEqual("GET",context.Request.HttpMethod);
            return Reply(context,404,"{\"code\":\"ObjectNotFound\",\"message\":\"controlled\"}");
        };
        Func<HttpListenerContext,Task> write=async context=>{
            Assert.AreEqual("PUT",context.Request.HttpMethod);puts++;
            using var reader=new StreamReader(context.Request.InputStream);
            var encrypted=await reader.ReadToEndAsync();Assert.IsFalse(encrypted.Contains("private-user",StringComparison.Ordinal));
            await Reply(context,partial&&puts==1?200:503,partial&&puts==1?"":"{\"code\":\"ServiceUnavailable\",\"message\":\"controlled\"}");
        };
        await Exchange(async _=>{
            using var client=Client();var storageKey=RandomNumberGenerator.GetBytes(32);
            try {
                using var provider=new OracleObjectStorageOASIS(client,"ns","bucket",()=>storageKey.ToArray());
                var avatar=new NextGenSoftware.OASIS.API.Core.Holons.Avatar{Username="private-user",Version=9};
                var originalKeys=avatar.ProviderUniqueStorageKey;
                var result=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
                var native=result.Exception;
                if(partial){
                    var acknowledged=(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValuePartialMutationException)native!;
                    Assert.AreEqual(1,acknowledged.AcknowledgedKeys.Count);native=acknowledged.InnerException;
                }
                Assert.IsInstanceOfType<OracleObjectMutationReconciliationException>(native);
                Assert.AreEqual(Guid.Empty,avatar.Id);Assert.AreEqual(9,avatar.Version);
                Assert.AreSame(originalKeys,avatar.ProviderUniqueStorageKey);Assert.AreEqual(partial?2:1,puts);
            }finally{CryptographicOperations.ZeroMemory(storageKey);}
        },partial?new[]{missing,missing,write,write}:new[]{missing,missing,write});
    }

    [DataTestMethod][DataRow(false,412)][DataRow(true,412)][DataRow(false,503)][DataRow(true,503)]
    public async Task PublicHolonCreateUsesNativePreconditionAndKeepsFailedProposalPrivate(bool synchronous,int status)
    {
        string? observedPath=null;
        await Exchange(async _=>{
            using var client=Client();var key=RandomNumberGenerator.GetBytes(32);
            try {
                using var provider=new OracleObjectStorageOASIS(client,"ns","bucket",()=>key.ToArray());
                var caller=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Name="private-failed-proposal",Version=9};var keys=caller.ProviderUniqueStorageKey;
                var result=synchronous?provider.SaveHolon(caller,saveChildren:false):await provider.SaveHolonAsync(caller,saveChildren:false);
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
                Assert.AreEqual(Guid.Empty,caller.Id);Assert.AreEqual(9,caller.Version);Assert.AreSame(keys,caller.ProviderUniqueStorageKey);
                Assert.AreEqual(status==503,NativeMutationReconciliation.IsRequired(result.Exception));
                if(status==412)Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);
                else Assert.IsInstanceOfType<OracleObjectMutationReconciliationException>(result.Exception);
            }finally{CryptographicOperations.ZeroMemory(key);}
        },async context=>{
            Assert.AreEqual("GET",context.Request.HttpMethod);observedPath=context.Request.Url!.AbsolutePath;
            await Reply(context,404,"{\"code\":\"ObjectNotFound\",\"message\":\"controlled\"}");
        },async context=>{
            Assert.AreEqual("PUT",context.Request.HttpMethod);Assert.AreEqual(observedPath,context.Request.Url!.AbsolutePath);
            Assert.AreEqual("*",context.Request.Headers["If-None-Match"]);Assert.IsNull(context.Request.Headers["If-Match"]);
            using var reader=new StreamReader(context.Request.InputStream);Assert.IsFalse((await reader.ReadToEndAsync()).Contains("private-failed-proposal",StringComparison.Ordinal));
            await Reply(context,status,"{\"code\":\"controlled\",\"message\":\"controlled\"}");
        });
    }

    [TestMethod]
    public async Task MissingPersistedEncryptionKeyFailsBeforeAnySdkRequest()
    {
        using var client=Client();
        using var provider=new OracleObjectStorageOASIS(client,"ns","bucket",()=>throw new InvalidOperationException("owned missing storage key"));
        var result=await provider.ActivateProviderAsync();Assert.IsTrue(result.IsError);Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsInstanceOfType<InvalidOperationException>(result.Exception);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task ConditionalEncryptionWrappersKeepNativeRevisionAndRejectStaleWrites(int layer)
    {
        string? ciphertext=null;var revision=0;
        void Address(HttpListenerContext context){
            var path=Uri.UnescapeDataString(context.Request.Url!.AbsolutePath);
            if(layer>0){
                Assert.IsFalse(path.Contains("conditional-private",StringComparison.Ordinal));
                StringAssert.Matches(path,new System.Text.RegularExpressions.Regex("/o/oasis/private/v1/[a-f0-9]{64}$"));
            }else StringAssert.EndsWith(path,"/o/oasis/conditional-private");
        }
        Func<HttpListenerContext,Task> write=async context=>{
            Address(context);
            Assert.AreEqual("PUT",context.Request.HttpMethod);
            Assert.AreEqual(revision==0?"*":null,context.Request.Headers["If-None-Match"]);
            Assert.AreEqual(revision==0?null:"\"rev-1\"",context.Request.Headers["If-Match"]);
            using var reader=new StreamReader(context.Request.InputStream);ciphertext=await reader.ReadToEndAsync();
            Assert.IsFalse(ciphertext.Contains("private-original",StringComparison.Ordinal));Assert.IsFalse(ciphertext.Contains("private-replacement",StringComparison.Ordinal));
            revision++;await Reply(context,200,"");
        };
        Func<HttpListenerContext,Task> read=context=>{
            Address(context);
            Assert.AreEqual("GET",context.Request.HttpMethod);
            if(ciphertext==null)return Reply(context,404,"{\"code\":\"ObjectNotFound\",\"message\":\"controlled\"}");
            context.Response.Headers["ETag"]="\"rev-"+revision+"\"";return Reply(context,200,ciphertext);
        };
        await Exchange(async native=>{
            var key=RandomNumberGenerator.GetBytes(32);
            try {
                IKeyValueBackend wrapped=layer switch {
                    0=>EncryptedKeyValueBackend.Create(native,key),
                    1=>PrivateKeyValueBackend.Create(native,key),
                    _=>PersistedPrivateKeyValueBackend.Create(native,()=>key.ToArray())};
                using var owner=(IDisposable)wrapped;var conditional=(IConditionalKeyValueBackend)wrapped;
                await conditional.CreateOnlyAsync("oasis/conditional-private","private-original");
                var original=await conditional.ReadObservedAsync("oasis/conditional-private");Assert.IsNotNull(original);
                Assert.AreEqual("private-original",original.Value);Assert.AreEqual("\"rev-1\"",original.Revision);
                await conditional.ReplaceUnchangedAsync("oasis/conditional-private","private-replacement",original.Revision);
                await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(()=>conditional.ReplaceUnchangedAsync("oasis/conditional-private","stale",original.Revision));
                var current=await conditional.ReadObservedAsync("oasis/conditional-private");Assert.IsNotNull(current);
                Assert.AreEqual("private-replacement",current.Value);Assert.AreEqual("\"rev-2\"",current.Revision);
                await conditional.DeleteUnchangedAsync("oasis/conditional-private",current.Revision);
                Assert.IsNull(await conditional.ReadObservedAsync("oasis/conditional-private"));
                owner.Dispose();await Assert.ThrowsExceptionAsync<ObjectDisposedException>(()=>conditional.ReadObservedAsync("oasis/conditional-private"));
            }finally{CryptographicOperations.ZeroMemory(key);}
        },write,read,write,async context=>{
            Address(context);
            Assert.AreEqual("PUT",context.Request.HttpMethod);Assert.AreEqual("\"rev-1\"",context.Request.Headers["If-Match"]);
            await Reply(context,412,"{\"code\":\"PreconditionFailed\",\"message\":\"controlled\"}");
        },read,async context=>{
            Address(context);
            Assert.AreEqual("DELETE",context.Request.HttpMethod);Assert.AreEqual("\"rev-2\"",context.Request.Headers["If-Match"]);
            ciphertext=null;await Reply(context,204,"");
        },read);
    }

    [TestMethod]
    public async Task PrivateConditionalUncertaintyRetainsNativeMarkerAndZeroesTemporaryKey()
    {
        await Exchange(async native=>{
            var key=RandomNumberGenerator.GetBytes(32);byte[]? resolved=null;
            try {
                using var wrapped=PersistedPrivateKeyValueBackend.Create(native,()=>resolved=key.ToArray());
                var conditional=(IConditionalKeyValueBackend)wrapped;
                var uncertain=await Assert.ThrowsExceptionAsync<OracleObjectMutationReconciliationException>(()=>conditional.CreateOnlyAsync("private-logical-name","private-value"));
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(uncertain));
                StringAssert.Matches(uncertain.ObjectName,new System.Text.RegularExpressions.Regex("^oasis/private/v1/[a-f0-9]{64}$"));
                Assert.IsNotNull(resolved);Assert.IsTrue(resolved.All(value=>value==0));
            }finally{CryptographicOperations.ZeroMemory(key);}
        },async context=>{
            Assert.AreEqual("PUT",context.Request.HttpMethod);Assert.AreEqual("*",context.Request.Headers["If-None-Match"]);
            await Reply(context,503,"{\"code\":\"ServiceUnavailable\",\"message\":\"controlled\"}");
        });
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
