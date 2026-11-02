using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Places;

public sealed class WorldscapePlace : PlaceVisitBase
{
    public double DistanceMetres { get; set; }
    public double[][] Boundary { get; set; }
}
public class PlaceVisitBase
{
    public string Website { get; set; }
    public string OpeningHours { get; set; }
    public string Phone { get; set; }
    public string MenuUrl { get; set; }
    public string Source { get; set; }
    public string SourceUrl { get; set; }
    public DateTime FetchedUtc { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class WorldscapePlaceCatalogue
{
    private readonly GeoapifyPlaceSource geoapify;
    public WorldscapePlaceCatalogue(IConfiguration configuration)
    { if(!string.IsNullOrWhiteSpace(configuration["GEOAPIFY_API_KEY"]))geoapify=new GeoapifyPlaceSource(configuration); }
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(25) };
    private readonly ConcurrentDictionary<string, (DateTime At, WorldscapePlace[] Places)> cache = new();
    private readonly SemaphoreSlim queryGate = new(1, 1);
    private DateTime nextQueryUtc;
    public static readonly Dictionary<string, string[]> Filters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Park"] = new[] { "[leisure=park]", "[leisure=nature_reserve]" },
        ["Forest"] = new[] { "[landuse=forest]" }, ["Beach"] = new[] { "[natural=beach]" },
        ["Woodland"] = new[] { "[natural=wood]" }, ["Farm"] = new[] { "[landuse=farmland]" },
        ["Mountain"] = new[] { "[natural=peak]" }, ["River"] = new[] { "[waterway=river]" },
        ["Lake"] = new[] { "[natural=water][water=lake]" }, ["Stream"] = new[] { "[waterway=stream]" },
        ["Yoga"] = new[] { "[sport~\"(^|;)yoga(;|$)\"]", "[name~\"yoga\",i]" },
        ["Meditation"] = new[] { "[name~\"meditation|meditaci[oó]n\",i]" },
        ["Mindfulness"] = new[] { "[name~\"mindfulness|mindful\",i]" },
        ["Ecstatic Dance"] = new[] { "[name~\"ecstatic dance|danza ext[aá]tica\",i]" },
        ["Vegan Food"] = new[] { "[amenity~\"^(cafe|restaurant|fast_food|food_court)$\"][\"diet:vegan\"~\"^(yes|only)$\"]" },
        ["Health Shops"] = new[] { "[shop=health_food]", "[shop~\"^(supermarket|convenience|greengrocer)$\"][organic=only]" }
    };
    public async Task<WorldscapePlace[]> Search(string category, double latitude, double longitude, CancellationToken token)
    {
        ValidateCoordinates(latitude, longitude);
        if (!Filters.ContainsKey(category) && category != "All") throw new ArgumentException("Unsupported place category.");
        string key = category + ":" + Math.Round(latitude, 3).ToString(CultureInfo.InvariantCulture) + ":" + Math.Round(longitude, 3).ToString(CultureInfo.InvariantCulture);
        if (!cache.TryGetValue(key, out var cached) || DateTime.UtcNow - cached.At > TimeSpan.FromMinutes(5))
        {
            await queryGate.WaitAsync(token);
            try
            {
                if (!cache.TryGetValue(key, out cached) || DateTime.UtcNow - cached.At > TimeSpan.FromMinutes(5))
                {
                    if(geoapify!=null)
                    {
                        cached=(DateTime.UtcNow,await geoapify.Search(category,latitude,longitude,token));
                        cache[key]=cached;
                    }
                    else
                    {
                    var wait = nextQueryUtc - DateTime.UtcNow;
                    if (wait > TimeSpan.Zero) await Task.Delay(wait, token);
                    nextQueryUtc = DateTime.UtcNow.AddSeconds(1);
                    var selected = category == "All" ? Filters : Filters.Where(p => p.Key.Equals(category, StringComparison.OrdinalIgnoreCase));
                    string origin = FormattableString.Invariant($"(around:5100,{latitude:R},{longitude:R})");
                    string query = "[out:json][timeout:20];(" + string.Join("", selected.SelectMany(p => p.Value).Distinct().Select(f => "nwr" + origin + f + ";")) + " );out body geom;";
                    using var request = new HttpRequestMessage(HttpMethod.Get, "https://overpass-api.de/api/interpreter?data=" + Uri.EscapeDataString(query));
                    request.Headers.UserAgent.ParseAdd("OurWorld-Worldscape/1.0 (+https://oasisweb4.com)");
                    using var response = await Http.SendAsync(request, token);
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync(token);
                    if (json.Length > 4_000_000) throw new InvalidOperationException("Nearby-place response exceeds its supported size.");
                    cached = (DateTime.UtcNow, Parse(json, category));
                    if (cache.Count >= 128) foreach (var old in cache.OrderBy(p => p.Value.At).Take(32)) cache.TryRemove(old.Key, out _);
                    cache[key] = cached;
                    }
                }
            }
            finally { queryGate.Release(); }
        }
        return cached.Places.Select(p => new WorldscapePlace { Id=p.Id,Name=p.Name,Category=p.Category,Website=p.Website,OpeningHours=p.OpeningHours,Phone=p.Phone,MenuUrl=p.MenuUrl,Source=p.Source,SourceUrl=p.SourceUrl,FetchedUtc=p.FetchedUtc,Latitude=p.Latitude,Longitude=p.Longitude,Boundary=p.Boundary,
            DistanceMetres=Distance(latitude,longitude,p.Latitude,p.Longitude) }).Where(p => p.DistanceMetres <= 5000).OrderBy(p => p.DistanceMetres).ThenBy(p=>p.Id).Take(200).ToArray();
    }
    public async Task<WorldscapePlace> Resolve(string id, double latitude, double longitude, CancellationToken token)
    {
        var known = cache.Values.Where(c => DateTime.UtcNow-c.At < TimeSpan.FromMinutes(5)).SelectMany(c=>c.Places).FirstOrDefault(p=>p.Id==id);
        if (known != null) return known;
        return (await Search("All",latitude,longitude,token)).SingleOrDefault(p=>p.Id==id)
            ?? throw new ArgumentException("Place was not found in the current catalogue. Refresh nearby places.");
    }
    public static WorldscapePlace[] Parse(string json, string category)
    {
        using var document=JsonDocument.Parse(json);
        if(document.RootElement.TryGetProperty("remark",out _))throw new InvalidOperationException("The place provider did not complete its query.");
        var result=new List<WorldscapePlace>();
        foreach(var row in document.RootElement.GetProperty("elements").EnumerateArray())
        {
            if(!row.TryGetProperty("tags",out var tags))continue;
            string name=tags.TryGetProperty("name",out var n)?n.GetString():null;

            var point=row.TryGetProperty("center",out var center)?center:row;
            double[][] boundary = null;
            if(row.TryGetProperty("geometry",out var geometry))
                boundary=geometry.EnumerateArray().Where(p=>p.TryGetProperty("lat",out _)&&p.TryGetProperty("lon",out _))
                    .Select(p=>new[]{p.GetProperty("lat").GetDouble(),p.GetProperty("lon").GetDouble()}).ToArray();
            double latitude,longitude;
            if(point.TryGetProperty("lat",out var lat)&&point.TryGetProperty("lon",out var lon)){latitude=lat.GetDouble();longitude=lon.GetDouble();}
            else if(boundary!=null&&boundary.Length>0){latitude=(boundary.Min(p=>p[0])+boundary.Max(p=>p[0]))/2;longitude=(boundary.Min(p=>p[1])+boundary.Max(p=>p[1]))/2;}
            else if(row.TryGetProperty("bounds",out var bounds)){latitude=(bounds.GetProperty("minlat").GetDouble()+bounds.GetProperty("maxlat").GetDouble())/2;longitude=(bounds.GetProperty("minlon").GetDouble()+bounds.GetProperty("maxlon").GetDouble())/2;}
            else continue;
            string kind=category=="All"?Classify(tags):category;
            if(kind==null)continue;
            if(string.IsNullOrWhiteSpace(name))name="Unnamed "+kind.ToLowerInvariant();
            string Tag(string key)=>tags.TryGetProperty(key,out var x)?x.GetString():null;
            result.Add(new WorldscapePlace {Website=Tag("website"),OpeningHours=Tag("opening_hours"),Phone=Tag("phone"),MenuUrl=Tag("website:menu"),Source="OpenStreetMap",SourceUrl="https://www.openstreetmap.org/copyright",FetchedUtc=DateTime.UtcNow,Boundary=boundary,Id="osm:"+row.GetProperty("type").GetString()+":"+row.GetProperty("id").GetInt64(),Name=name,Category=kind,Latitude=latitude,Longitude=longitude});
        }
        return result.GroupBy(p=>p.Id).Select(g=>g.First()).ToArray();
    }
    internal static string Classify(JsonElement tags)
    {
        string Get(string key)=>tags.TryGetProperty(key,out var value)?value.GetString()??"":"";
        string name=Get("name").ToLowerInvariant();
        if(Get("diet:vegan") is "yes" or "only")return "Vegan Food";
        if(Get("shop")=="health_food"||Get("organic")=="only")return "Health Shops";
        if(name.Contains("ecstatic")||name.Contains("extática"))return "Ecstatic Dance";
        if(name.Contains("mindful"))return "Mindfulness";
        if(name.Contains("meditat")||name.Contains("meditaci"))return "Meditation";
        if(Get("sport").Split(';').Contains("yoga")||name.Contains("yoga"))return "Yoga";
        if(Get("leisure") is "park" or "nature_reserve")return "Park";
        if(Get("landuse")=="forest")return "Forest";
        if(Get("natural")=="wood")return "Woodland";
        if(Get("natural")=="beach")return "Beach";
        if(Get("landuse")=="farmland")return "Farm";
        if(Get("natural")=="peak")return "Mountain";
        if(Get("waterway")=="river")return "River";
        if(Get("waterway")=="stream")return "Stream";
        if(Get("water")=="lake")return "Lake";
        return null;
    }
    public static void ValidateCoordinates(double latitude,double longitude)
    {if(!double.IsFinite(latitude)||!double.IsFinite(longitude)||latitude < -90||latitude>90||longitude < -180||longitude>180)throw new ArgumentException("Valid geographic coordinates are required.");}
    public static double DistanceToPlace(WorldscapePlace place,double latitude,double longitude)
    {
        var ring=place.Boundary;
        if(ring!=null&&ring.Length>=4&&ring[0][0]==ring[^1][0]&&ring[0][1]==ring[^1][1])
        {
            bool inside=false;
            for(int i=0,j=ring.Length-1;i<ring.Length;j=i++)
                if((ring[i][0]>latitude)!=(ring[j][0]>latitude)&&longitude<(ring[j][1]-ring[i][1])*(latitude-ring[i][0])/(ring[j][0]-ring[i][0])+ring[i][1])inside=!inside;
            if(inside)return 0;
        }
        return Distance(latitude,longitude,place.Latitude,place.Longitude);
    }
    public static double Distance(double a,double b,double c,double d)
    {double R(double x)=>x*Math.PI/180;double x=Math.Pow(Math.Sin(R(c-a)/2),2)+Math.Cos(R(a))*Math.Cos(R(c))*Math.Pow(Math.Sin(R(d-b)/2),2);return 6371000*2*Math.Asin(Math.Sqrt(Math.Clamp(x,0,1)));}
}
