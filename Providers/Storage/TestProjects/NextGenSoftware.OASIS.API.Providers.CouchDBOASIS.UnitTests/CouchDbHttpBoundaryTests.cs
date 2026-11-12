using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.CouchDBOASIS.UnitTests;

[TestClass]
public class CouchDbHttpBoundaryTests
{
    private sealed class Handler : HttpMessageHandler
    {
        public HttpStatusCode ReadStatus=HttpStatusCode.NotFound;
        public string ReadBody="{}";
        public bool FailIndex;
        public int DocumentWrites;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            if(request.Method==HttpMethod.Get) return Task.FromResult(new HttpResponseMessage(ReadStatus){Content=new StringContent(ReadBody)});
            if(request.RequestUri!.AbsolutePath.EndsWith("/_index")) return Task.FromResult(new HttpResponseMessage(FailIndex?HttpStatusCode.InternalServerError:HttpStatusCode.Created){Content=new StringContent("{}")});
            if(request.RequestUri.AbsolutePath.Count(c=>c=='/')>1)DocumentWrites++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created){Content=new StringContent(System.Text.Json.JsonSerializer.Serialize(new{ok=true,id=request.RequestUri.Segments.Last(),rev="1-a"}),Encoding.UTF8,"application/json")});
        }
    }
    private static CouchDBOASIS Provider(Handler handler)
    {
        var provider=new CouchDBOASIS("http://127.0.0.1:1");
        var field=typeof(CouchDBOASIS).GetField("_http",BindingFlags.Instance|BindingFlags.NonPublic)!;
        ((HttpClient)field.GetValue(provider)!).Dispose();field.SetValue(provider,new HttpClient(handler));return provider;
    }
    [DataTestMethod][DataRow(401)][DataRow(403)][DataRow(500)]
    public async Task RevisionReadFailureNeverBecomesCreate(int status)
    {
        var h=new Handler{ReadStatus=(HttpStatusCode)status};var result=await Provider(h).SaveHolonAsync(new Holon{Id=Guid.NewGuid()},saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.AreEqual(0,h.DocumentWrites);
    }
    [TestMethod]
    public async Task ExistingDocumentWithoutRevisionRejectsBeforeWrite()
    {
        var h=new Handler{ReadStatus=HttpStatusCode.OK,ReadBody="{}"};
        var result=await Provider(h).SaveHolonAsync(new Holon{Id=Guid.NewGuid()},saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.AreEqual(0,h.DocumentWrites);
    }
    [TestMethod]
    public async Task ActualNotFoundMayCreate()
    {
        var h=new Handler();var result=await Provider(h).SaveHolonAsync(new Holon{Id=Guid.NewGuid()},saveChildren:false);
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(1,h.DocumentWrites);
    }
    [TestMethod]
    public async Task FailedIndexCreationDoesNotActivate()
    {
        var h=new Handler{FailIndex=true};var p=Provider(h);var result=await p.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);Assert.IsFalse(p.IsProviderActivated);
    }
    [TestMethod]
    public async Task HardDeleteReadFailureDoesNotReportSuccess()
    {
        var h=new Handler{ReadStatus=HttpStatusCode.Unauthorized};
        var result=await Provider(h).DeleteHolonSoftAsync(Guid.NewGuid(),softDelete:false);
        Assert.IsTrue(result.IsError);Assert.AreEqual(0,h.DocumentWrites);
    }
}
