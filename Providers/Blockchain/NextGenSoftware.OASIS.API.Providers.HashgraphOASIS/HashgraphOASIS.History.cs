using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public partial class HashgraphOASIS
{
    private sealed record MirrorHistoryRange(decimal Minimum,decimal Maximum,bool Empty=false);
    private sealed record MirrorHistoryBlock(ulong Number,decimal From,decimal To);

    private static decimal ParseMirrorHistoryTimestamp(string text)
    {
        if(!System.Text.RegularExpressions.Regex.IsMatch(text??"",@"\A[0-9]+\.[0-9]{9}\z")
            ||!decimal.TryParse(text,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value))
            throw new InvalidOperationException("Hedera mirror has an invalid exact consensus timestamp.");
        return value;
    }
    private static MirrorHistoryBlock ReadMirrorHistoryBlock(JsonElement block,ulong? expected=null)
    {
        var number=block.GetProperty("number").GetUInt64();
        if(expected.HasValue&&number!=expected.Value)throw new InvalidOperationException("Mirror block identity differs from the requested block.");
        var interval=block.GetProperty("timestamp");
        var from=ParseMirrorHistoryTimestamp(interval.GetProperty("from").GetString());
        var to=ParseMirrorHistoryTimestamp(interval.GetProperty("to").GetString());
        if(from>to)throw new InvalidOperationException("Mirror block timestamp interval is reversed.");
        return new(number,from,to);
    }
    private async Task<MirrorHistoryRange> ResolveMirrorHistoryRangeAsync(Uri endpoint,ulong? fromBlock,ulong? toBlock,decimal cutoff,CancellationToken token)
    {
        if(!fromBlock.HasValue&&!toBlock.HasValue)return new(0,cutoff);
        using var response=await _httpClient.GetAsync(new Uri(endpoint,"blocks?limit=1&order=desc"),token);response.EnsureSuccessStatusCode();
        using var latestJson=JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        var blocks=latestJson.RootElement.GetProperty("blocks");
        if(blocks.ValueKind!=JsonValueKind.Array||blocks.GetArrayLength()!=1)throw new InvalidOperationException("A unique latest indexed mirror block is required.");
        var latest=ReadMirrorHistoryBlock(blocks[0]);
        if(fromBlock.HasValue&&fromBlock.Value>latest.Number)return new(0,0,true);
        async Task<MirrorHistoryBlock> Load(ulong number){
            if(number==latest.Number)return latest;
            using var blockResponse=await _httpClient.GetAsync(new Uri(endpoint,"blocks/"+number.ToString(CultureInfo.InvariantCulture)),token);
            blockResponse.EnsureSuccessStatusCode(); // Missing/pruned metadata is NOT proof of an empty interval.
            using var blockJson=JsonDocument.Parse(await blockResponse.Content.ReadAsStringAsync(token));
            var block=ReadMirrorHistoryBlock(blockJson.RootElement,number);
            if(block.To>latest.To)throw new InvalidOperationException("Mirror block exceeds the fixed latest block timestamp.");
            return block;
        }
        var end=await Load(toBlock.HasValue&&toBlock.Value<latest.Number?toBlock.Value:latest.Number);
        var start=fromBlock.HasValue?(fromBlock.Value==end.Number?end:await Load(fromBlock.Value)):null;
        var minimum=start?.From??0;var maximum=Math.Min(cutoff,end.To);
        if(minimum>maximum)throw new InvalidOperationException("Mirror block range timestamps are inconsistent or in the future.");
        return new(minimum,maximum);
    }

    private static Uri ConstrainMirrorHistoryPage(Uri page,decimal minimum,string upperBound)
    {
        var query=page.Query.TrimStart('?').Split('&').Select(Uri.UnescapeDataString).ToArray();
        var upper=query.Where(value=>value.StartsWith("timestamp=lte:",StringComparison.Ordinal)).ToArray();
        if(upper.Length>1||upper.Length==1&&upper[0]!=upperBound)throw new InvalidOperationException("Hedera history pagination changed the fixed scan timestamp.");
        if(upper.Length==0)page=new Uri(page.AbsoluteUri+(page.Query.Length==0?"?":"&")+upperBound);
        var lower=query.Where(value=>value.StartsWith("timestamp=gte:",StringComparison.Ordinal)||value.StartsWith("timestamp=gt:",StringComparison.Ordinal)).ToArray();
        if(lower.Length>1)throw new InvalidOperationException("Hedera history pagination has ambiguous lower range bounds.");
        if(lower.Length==1){
            var text=lower[0][(lower[0].IndexOf(':')+1)..];
            var value=text=="0"?0:ParseMirrorHistoryTimestamp(text);
            if(value<minimum)throw new InvalidOperationException("Hedera history pagination weakened the requested block range.");
        }else page=new Uri(page.AbsoluteUri+(page.Query.Length==0?"?":"&")+"timestamp=gte:"+(minimum==0?"0":minimum.ToString("0.000000000",CultureInfo.InvariantCulture)));
        return page;
    }
}
