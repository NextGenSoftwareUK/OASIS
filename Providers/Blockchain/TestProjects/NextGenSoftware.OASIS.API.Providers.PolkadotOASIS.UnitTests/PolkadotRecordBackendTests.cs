using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public class PolkadotRecordBackendTests
{
    private const string Identity = "0x1111111111111111111111111111111111111111111111111111111111111111";
    private static JsonElement Reply(object observation) => JsonSerializer.SerializeToElement(new {
        blockHash = Identity, contractAddress = Identity, codeHash = Identity, observation });
    private static PolkadotRecordBackend Backend(Func<object,CancellationToken,PreparedSubmissionServer,Task<JsonElement>> invoke)
        => new("http://127.0.0.1:19944",Identity,Identity,Identity,Identity,"0x"+new string('2',64),invoke);

    [TestMethod]
    public async Task ObservationPreservesExactUtf8AndNativeRevision()
    {
        JsonElement sent = default;
        var backend = Backend((request,_,server) => {
            Assert.IsNull(server); sent=JsonSerializer.SerializeToElement(request);
            return Task.FromResult(Reply(new { valueBase64=Convert.ToBase64String(Encoding.UTF8.GetBytes("record-🌳")),revision="9007199254740993" }));
        });
        var found=await backend.ReadObservedAsync("key-🌳");
        Assert.AreEqual("record-🌳",found.Value); Assert.AreEqual("9007199254740993",found.Revision);
        Assert.AreEqual("recordRead",sent.GetProperty("operation").GetString());
        Assert.AreEqual("key-🌳",Encoding.UTF8.GetString(Convert.FromBase64String(sent.GetProperty("keyBase64").GetString()!)));
    }
    [TestMethod]
    public async Task OnlyExplicitNullMeansAbsent()
    {
        Assert.IsNull(await Backend((_,_,_)=>Task.FromResult(Reply(null!))).GetAsync("key"));
        var bad=Backend((_,_,_)=>Task.FromResult(Reply(new { valueBase64="YQ==",revision="01" })));
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>bad.GetAsync("key"));
        var identity=Backend((_,_,_)=>Task.FromResult(JsonSerializer.SerializeToElement(new { blockHash=Identity,contractAddress=Identity,codeHash="bad",observation=(object?)null })));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>identity.GetAsync("key"));
    }
    [TestMethod]
    public async Task DenseEnumerationSortsWithoutDiscardingKeys()
    {
        var backend=Backend((_,_,_)=>Task.FromResult(JsonSerializer.SerializeToElement(new {
            blockHash=Identity,contractAddress=Identity,codeHash=Identity,generation="7",
            keysBase64=new[]{"p:b","p:a"}.Select(k=>Convert.ToBase64String(Encoding.UTF8.GetBytes(k))).ToArray() })));
        CollectionAssert.AreEqual(new[]{"p:a","p:b"},(await backend.ListKeysAsync("p:")).ToArray());
    }
    [DataTestMethod]
    [DataRow("p:a","p:a")]
    [DataRow("p:a","other")]
    public async Task InvalidEnumerationDoesNotPublishPartialResult(string first,string second)
    {
        var backend=Backend((_,_,_)=>Task.FromResult(JsonSerializer.SerializeToElement(new {
            blockHash=Identity,contractAddress=Identity,codeHash=Identity,generation="1",
            keysBase64=new[]{first,second}.Select(k=>Convert.ToBase64String(Encoding.UTF8.GetBytes(k))).ToArray() })));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>backend.ListKeysAsync("p:"));
    }
    [TestMethod]
    public async Task WritesWithoutDurableOwnerFailBeforeSdk()
    {
        var calls=0;var backend=Backend((_,_,_)=>{calls++;throw new AssertFailedException("Unexpected SDK I/O");});
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>backend.PutAsync("key","value"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>backend.DeleteAsync("key"));
        Assert.AreEqual(0,calls);
    }
    [TestMethod]
    public async Task AdmissionFailureDoesNotCreatePipeOrCallSdk()
    {
        var calls=0;var factories=0;
        var backend=Backend((_,_,_)=>{calls++;throw new AssertFailedException("Unexpected SDK I/O");});
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>backend.WithPreparationAsync(async()=>{
            await backend.CreateOnlyAsync("key",new string('x',8193));return true;
        },(_,_)=>{factories++;throw new AssertFailedException("Unexpected pipe");}));
        Assert.AreEqual(0,calls);Assert.AreEqual(0,factories);
    }
    [TestMethod]
    public async Task ClosedOwnerCannotBeReusedByInheritedFlow()
    {
        var backend=Backend((_,_,_)=>throw new AssertFailedException("Unexpected SDK I/O"));
        var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? inherited=null;
        await backend.WithPreparationAsync(()=>{
            inherited=Task.Run(async()=>{await release.Task;await backend.PutAsync("key","value");});
            return Task.FromResult(true);
        },(_,_)=>throw new AssertFailedException("Unexpected pipe"));
        release.SetResult();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async()=>await inherited!);
    }
}
