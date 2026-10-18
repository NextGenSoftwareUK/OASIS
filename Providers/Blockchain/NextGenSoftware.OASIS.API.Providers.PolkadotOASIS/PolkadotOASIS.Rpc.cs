using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    private static bool IsGenesisHash(string value)=>value!=null && value.Length==66
        && value.StartsWith("0x",StringComparison.Ordinal) && value.Skip(2).All(Uri.IsHexDigit);

    private async Task<JsonElement> QueryRpcAsync(string method,object[] parameters)
    {
        var id=Interlocked.Increment(ref _rpcRequestId);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var request=new HttpRequestMessage(HttpMethod.Post,_rpcEndpoint)
        {Content=new StringContent(JsonSerializer.Serialize(new{jsonrpc="2.0",id,method,@params=parameters}),Encoding.UTF8,"application/json")};
        using var response=await _httpClient.SendAsync(request,deadline.Token);
        response.EnsureSuccessStatusCode();
        using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync(deadline.Token));
        var root=document.RootElement;
        if(root.ValueKind!=JsonValueKind.Object || root.EnumerateObject().GroupBy(p=>p.Name).Any(group=>group.Count()!=1)
            || !root.TryGetProperty("jsonrpc",out var protocol) || protocol.ValueKind!=JsonValueKind.String || protocol.GetString()!="2.0"
            || !root.TryGetProperty("id",out var correlation) || !correlation.TryGetInt32(out var returnedId) || returnedId!=id)
            throw new InvalidDataException("Polkadot RPC response envelope does not match the request.");
        if(root.TryGetProperty("error",out var error) && error.ValueKind!=JsonValueKind.Null)
            throw new InvalidDataException("Polkadot RPC returned an error.");
        if(!root.TryGetProperty("result",out var result) || result.ValueKind==JsonValueKind.Null)
            throw new InvalidDataException("Polkadot RPC response has no authoritative result.");
        return result.Clone();
    }
}
