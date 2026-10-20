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
}
