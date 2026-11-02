using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Places;

/// <summary>On-demand geometry for a selected OSM feature, rather than treating an area's centre as its boundary.</summary>
public static class OsmPlaceGeometry
{
    private static readonly HttpClient Http=new(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(15)};
    private static readonly SemaphoreSlim Gate=new(1,1);
    private static DateTime nextRequest;
    private static readonly ConcurrentDictionary<string,(DateTime At,double[][][] Outer,double[][][] Inner)> Cache=new();
    public static async Task Enrich(WorldscapePlace place,CancellationToken token)
    {
        string[] id=place.Id.Split(':');
        if(id.Length!=3||id[0]!="osm"||!long.TryParse(id[2],NumberStyles.None,CultureInfo.InvariantCulture,out long number)||number<1)
            throw new InvalidOperationException("This place has no supported public map identity for boundary verification.");
        if(id[1]=="node")return;
        if(id[1]!="way"&&id[1]!="relation")throw new InvalidOperationException("Unsupported place geometry.");
        if(!Cache.TryGetValue(place.Id,out var geometry)||DateTime.UtcNow-geometry.At>TimeSpan.FromHours(24))
        {
            await Gate.WaitAsync(token);
            try
            {
                if(!Cache.TryGetValue(place.Id,out geometry)||DateTime.UtcNow-geometry.At>TimeSpan.FromHours(24))
                {
                    var delay=nextRequest-DateTime.UtcNow;if(delay>TimeSpan.Zero)await Task.Delay(delay,token);
                    nextRequest=DateTime.UtcNow.AddSeconds(1);
                    using var request=new HttpRequestMessage(HttpMethod.Get,$"https://api.openstreetmap.org/api/0.6/{id[1]}/{number}/full.json");
                    request.Headers.UserAgent.ParseAdd("OurWorld-Gardens/1.0 (+https://oasisweb4.com)");
                    using var response=await Http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();
                    if(response.Content.Headers.ContentLength>2_000_000)throw new InvalidOperationException("Selected place geometry is too large.");
                    string json=await response.Content.ReadAsStringAsync(token);if(json.Length>2_000_000)throw new InvalidOperationException("Selected place geometry is too large.");
                    var parsed=Parse(json,id[1],number);geometry=(DateTime.UtcNow,parsed.Outer,parsed.Inner);
                    if(Cache.Count>=256)foreach(var old in Cache.OrderBy(p=>p.Value.At).Take(64))Cache.TryRemove(old.Key,out _);
                    Cache[place.Id]=geometry;
                }
            }
            finally{Gate.Release();}
        }
        place.OuterBoundaries=geometry.Outer;place.InnerBoundaries=geometry.Inner;
        if(geometry.Outer.Length==1)place.Boundary=geometry.Outer[0];
    }
    public static (double[][][] Outer,double[][][] Inner) Parse(string json,string kind,long id)
    {
        using var document=JsonDocument.Parse(json);var elements=document.RootElement.GetProperty("elements").EnumerateArray().ToArray();
        var nodes=elements.Where(e=>e.GetProperty("type").GetString()=="node").ToDictionary(e=>e.GetProperty("id").GetInt64(),e=>new[]{e.GetProperty("lat").GetDouble(),e.GetProperty("lon").GetDouble()});
        var ways=elements.Where(e=>e.GetProperty("type").GetString()=="way").ToDictionary(e=>e.GetProperty("id").GetInt64(),e=>e.GetProperty("nodes").EnumerateArray().Select(n=>n.GetInt64()).ToList());
        double[][] Points(List<long> sequence)=>sequence.Select(n=>nodes.TryGetValue(n,out var point)?point:throw new InvalidOperationException("Map boundary has missing nodes.")).ToArray();
        if(kind=="way")return(new[]{Points(ways[id])},Array.Empty<double[][]>());
        var relation=elements.Single(e=>e.GetProperty("type").GetString()=="relation"&&e.GetProperty("id").GetInt64()==id);
        double[][][] Rings(string role)
        {
            var pieces=relation.GetProperty("members").EnumerateArray().Where(m=>m.GetProperty("type").GetString()=="way"&&m.GetProperty("role").GetString()==role)
                .Select(m=>new List<long>(ways[m.GetProperty("ref").GetInt64()])).ToList();var rings=new List<double[][]>();
            while(pieces.Count>0){var ring=pieces[0];pieces.RemoveAt(0);
                while(ring[0]!=ring[^1]){int next=pieces.FindIndex(p=>p[0]==ring[^1]||p[^1]==ring[^1]);if(next<0)throw new InvalidOperationException("Map area has an incomplete boundary.");var piece=pieces[next];pieces.RemoveAt(next);if(piece[^1]==ring[^1])piece.Reverse();ring.AddRange(piece.Skip(1));}
                rings.Add(Points(ring));}
            return rings.ToArray();
        }
        var outer=Rings("outer");if(outer.Length==0)throw new InvalidOperationException("This relation has no supported area boundary.");return(outer,Rings("inner"));
    }
}
