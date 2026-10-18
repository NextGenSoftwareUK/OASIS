using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

public partial class HederaWalletHistoryTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task PublicMirrorBlockRangeUsesExactNanosecondBoundaries(bool synchronous)
    {
        using var handler=new BlockRangeHandler();var provider=Provider(handler);
        var request=new GetWeb3TransactionsRequest{WalletAddress="0.0.1001",FromBlock=42,ToBlock=42};
        var result=synchronous?provider.GetTransactions(request):await provider.GetTransactionsAsync(request);
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(2,result.Result.Count);
        Assert.AreEqual(1,handler.Requests.Count(uri=>uri.AbsolutePath.EndsWith("/blocks/42")));
        var query=handler.Requests.Single(uri=>uri.AbsolutePath.EndsWith("/transactions")).Query;
        StringAssert.Contains(Uri.UnescapeDataString(query),"timestamp=gte:1700000000.100000000");
        StringAssert.Contains(Uri.UnescapeDataString(query),"timestamp=lte:1700000000.200000000");
    }

    [TestMethod]
    public async Task MirrorHistoryDoesNotRequirePaidStorageActivation()
    {
        using var handler=new BlockRangeHandler();var provider=Provider(handler);provider.IsProviderActivated=false;
        var result=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress="0.0.1001",FromBlock=42,ToBlock=42});
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(2,result.Result.Count);Assert.IsFalse(provider.IsProviderActivated);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ReversedMirrorBlockRangeFailsBeforeHttp(bool synchronous)
    {
        using var handler=new BlockRangeHandler();var provider=Provider(handler);
        var request=new GetWeb3TransactionsRequest{WalletAddress="0.0.1001",FromBlock=43,ToBlock=42};
        var result=synchronous?provider.GetTransactions(request):await provider.GetTransactionsAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);Assert.AreEqual(0,handler.Requests.Count);
    }

    [DataTestMethod]
    [DataRow("1700000000.099999999")] [DataRow("1700000000.200000001")]
    public async Task OutsideRequestedBlockByOneNanosecondCannotReturnPartialHistory(string timestamp)
    {
        using var handler=new BlockRangeHandler{OutsideTimestamp=timestamp};
        var result=await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress="0.0.1001",FromBlock=42,ToBlock=42});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);StringAssert.Contains(result.Message,"range");
    }

    [DataTestMethod]
    [DataRow("wrong-number")] [DataRow("bad-time")] [DataRow("missing")]
    public async Task UnverifiedBlockMetadataCannotBecomeEmptyOrSuccessfulHistory(string mode)
    {
        using var handler=new BlockRangeHandler{Mode=mode};
        var result=await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress="0.0.1001",FromBlock=42,ToBlock=42});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(handler.Requests.Any(uri=>uri.AbsolutePath.EndsWith("/transactions")));
    }

    [TestMethod]
    public async Task FutureU64RangeIsEmptyOnlyAfterVerifiedLatestMirrorBlock()
    {
        using var handler=new BlockRangeHandler();
        var result=await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress="0.0.1001",FromBlock=ulong.MaxValue});
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(0,result.Result.Count);Assert.AreEqual(1,handler.Requests.Count);
        StringAssert.EndsWith(handler.Requests[0].AbsolutePath,"/blocks");
    }

    [DataTestMethod]
    [DataRow("42","","1700000000.100000000","1700000009.999999999")]
    [DataRow("","42","0","1700000000.200000000")]
    [DataRow("42","18446744073709551615","1700000000.100000000","1700000009.999999999")]
    public async Task OptionalEndsAndFutureUpperClampUseFixedMirrorBounds(string from,string to,string minimum,string maximum)
    {
        using var handler=new BlockRangeHandler();
        var result=await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress="0.0.1001",
            FromBlock=from.Length==0?null:ulong.Parse(from),ToBlock=to.Length==0?null:ulong.Parse(to)});
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(2,result.Result.Count);
        var query=Uri.UnescapeDataString(handler.Requests.Single(uri=>uri.AbsolutePath.EndsWith("/transactions")).Query);
        StringAssert.Contains(query,"timestamp=gte:"+minimum);StringAssert.Contains(query,"timestamp=lte:"+maximum);
    }

    [TestMethod]
    public async Task MirrorBlockZeroUsesItsMetadataInsteadOfBeingAssumedEmpty()
    {
        using var handler=new BlockRangeHandler{Mode="zero"};
        var result=await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress="0.0.1001",FromBlock=0,ToBlock=0});
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(0,result.Result.Count);
        Assert.IsTrue(handler.Requests.Any(uri=>uri.AbsolutePath.EndsWith("/blocks/0")));
        StringAssert.Contains(Uri.UnescapeDataString(handler.Requests.Last().Query),"timestamp=lte:1699999990.999999999");
    }

    [TestMethod]
    public async Task CallerMutationDuringBlockLookupCannotChangeOriginalWalletOrBounds()
    {
        var request=new GetWeb3TransactionsRequest{WalletAddress="0.0.1001",FromBlock=42,ToBlock=42};
        using var handler=new BlockRangeHandler{OnBlockLookup=()=>{request.WalletAddress="0.0.9999";request.FromBlock=99;request.ToBlock=100;}};
        var result=await Provider(handler).GetTransactionsAsync(request);
        Assert.IsFalse(result.IsError,result.Message);
        Assert.IsTrue(handler.Requests.Any(uri=>uri.AbsolutePath.EndsWith("/blocks/42")));
        StringAssert.Contains(handler.Requests.Last().Query,"account.id=0.0.1001");
    }

    [TestMethod]
    public async Task BlockRangeAndCursorBoundsSurvivePagination()
    {
        using var handler=new BlockRangeHandler{Mode="paged"};
        var result=await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress="0.0.1001",FromBlock=42,ToBlock=42});
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(2,result.Result.Count);
        var pages=handler.Requests.Where(uri=>uri.AbsolutePath.EndsWith("/transactions")).ToArray();Assert.AreEqual(2,pages.Length);
        Assert.IsTrue(pages.All(uri=>Uri.UnescapeDataString(uri.Query).Contains("timestamp=lte:1700000000.200000000")));
    }

    [DataTestMethod]
    [DataRow("weaken-lower")] [DataRow("weaken-upper")]
    public async Task PaginationCannotWidenRequestedBlockRange(string mode)
    {
        using var handler=new BlockRangeHandler{Mode=mode};
        var result=await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress="0.0.1001",FromBlock=42,ToBlock=42});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        Assert.AreEqual(1,handler.Requests.Count(uri=>uri.AbsolutePath.EndsWith("/transactions")));
    }

    private sealed class BlockRangeHandler:HttpMessageHandler
    {
        public List<Uri> Requests {get;}=new();public string Mode="";public string? OutsideTimestamp;public Action? OnBlockLookup;private int _pages;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();var uri=request.RequestUri!;Requests.Add(uri);string body;
            object Block(ulong number,string from,string to)=>new{number,timestamp=new{from,to}};
            if(uri.AbsolutePath.EndsWith("/blocks")){OnBlockLookup?.Invoke();body=JsonSerializer.Serialize(new{blocks=new[]{Block(100,"1700000009.000000000","1700000009.999999999")}});}
            else if(uri.AbsolutePath.EndsWith("/blocks/0"))body=JsonSerializer.Serialize(Block(0,"1699999990.000000000","1699999990.999999999"));
            else if(uri.AbsolutePath.EndsWith("/blocks/42")){
                if(Mode=="missing")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                body=JsonSerializer.Serialize(Block(Mode=="wrong-number"?43u:42u,Mode=="bad-time"?"bad":"1700000000.100000000","1700000000.200000000"));
            }else if(uri.AbsolutePath.EndsWith("/transactions")){
                _pages++;var first=Record.Replace("1700000000.123456789",OutsideTimestamp??"1700000000.100000000");
                var last=Record.Replace("1700000000.123456789","1700000000.200000000").Replace("000000001\"","000000002\"");
                var next=_pages==1&&Mode is "paged" or "weaken-lower" or "weaken-upper"
                    ?"/api/v1/transactions?account.id=0.0.1001&timestamp=gt:"+(Mode=="weaken-lower"?"1700000000.099999999":"1700000000.100000000")
                        +(Mode=="weaken-upper"?"&timestamp=lte:1700000000.300000000":""):null;
                body=Page(Mode=="zero"?Array.Empty<string>():Mode=="paged"?new[]{_pages==1?first:last}:new[]{first,last},next);
            }else throw new InvalidOperationException("Unexpected mirror route: "+uri.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body)});
        }
    }
}
