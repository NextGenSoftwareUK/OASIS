using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Places;

/// <summary>One configured free-tier source. There is no paid upgrade or failure-triggered provider switch.</summary>
public sealed class GeoapifyPlaceSource
{
    private readonly string key;
    private readonly MongoPlaceCreditBudget budget;
    private static readonly HttpClient Http = new() { Timeout=TimeSpan.FromSeconds(20) };
    public GeoapifyPlaceSource(IConfiguration configuration)
    {
        key=configuration["GEOAPIFY_API_KEY"];
        if(string.IsNullOrWhiteSpace(key))throw new InvalidOperationException("Geoapify's free-project key is not configured.");
        budget=new MongoPlaceCreditBudget(new MongoClient(SubscriptionMongoConfiguration.ConnectionString(configuration)).GetDatabase("oasis_places")
            .GetCollection<BsonDocument>("provider_daily_budget"));
    }
    public async Task<WorldscapePlace[]> Search(string category,double latitude,double longitude,CancellationToken token)
    {
        // <=100 results costs at most5 credits. The majority-acknowledged shared counter
        // reserves those credits before I/O, including failures, across every API replica.
        const int limit=100;
        var (categories,name,conditions)=Parameters(category);
        await budget.Reserve(5,token);
        string body=JsonSerializer.Serialize(new {categories=categories.Split(','),name,conditions=conditions==null?null:new[]{conditions},limit,
            filter=new {type="circle",lon=longitude,lat=latitude,radius=5000},bias=new {type="proximity",lon=longitude,lat=latitude}},new JsonSerializerOptions {DefaultIgnoreCondition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull});
        using var request=new HttpRequestMessage(HttpMethod.Post,"https://api.geoapify.com/v2/places");
        request.Headers.Add("x-api-key",key);request.Content=new StringContent(body,Encoding.UTF8,"application/json");
        using var response=await Http.SendAsync(request,token);response.EnsureSuccessStatusCode();
        string json=await response.Content.ReadAsStringAsync(token);
        if(json.Length>4_000_000)throw new InvalidOperationException("Place response exceeds the supported size.");
        return Parse(json,category);
    }
    public static (string Categories,string Name,string Conditions) Parameters(string category)=>category switch
    {
        "Park"=>("leisure.park,natural.protected_area",null,null),
        "Forest" or "Woodland"=>("natural.forest",null,null),
        "Beach"=>("beach",null,null),"Mountain"=>("natural.mountain",null,null),
        "Lake" or "River" or "Stream"=>("natural.water,waterway",null,null),
        "Farm"=>("commercial.food_and_drink.farm","farm",null),
        "Yoga"=>("sport,healthcare,education,leisure","yoga",null),
        "Meditation"=>("sport,healthcare,education,leisure,religion","meditation",null),
        "Mindfulness"=>("sport,healthcare,education,leisure","mindful",null),
        "Ecstatic Dance"=>("entertainment,sport,education","ecstatic",null),
        "Vegan Food"=>("catering.restaurant,catering.cafe,catering.fast_food",null,"vegan"),
        "Health Shops"=>("commercial.food_and_drink.health_food,commercial.food_and_drink.organic",null,null),
        "All"=>("leisure.park,natural,sport,healthcare,education,entertainment,catering,commercial.food_and_drink",null,null),
        _=>throw new ArgumentException("Unsupported place category.")
    };
    public static WorldscapePlace[] Parse(string json,string category)
    {
        using var document=JsonDocument.Parse(json);
        var result=new System.Collections.Generic.List<WorldscapePlace>();
        foreach(var feature in document.RootElement.GetProperty("features").EnumerateArray())
        {
            var p=feature.GetProperty("properties");
            var raw=p.TryGetProperty("datasource",out var source)&&source.TryGetProperty("raw",out var r)?r:p;
            string actual=WorldscapePlaceCatalogue.Classify(raw);
            if(category=="All"&&actual==null)continue;
            if(category is "River" or "Lake" or "Stream" or "Farm" or "Forest" or "Woodland")
                if(actual!=category)continue; // Broad provider categories must not relabel a lake as a river/farm.
            var coordinates=feature.GetProperty("geometry").GetProperty("coordinates");
            string type=raw.TryGetProperty("osm_type",out var ot)?ot.ToString():null;
            type=type switch {"n"=>"node","w"=>"way","r"=>"relation",_=>type};
            string id=raw.TryGetProperty("osm_id",out var osm)&&type is "node" or "way" or "relation"
                ?"osm:"+type+":"+osm.ToString():"geoapify:"+p.GetProperty("place_id").GetString();
            string name=p.TryGetProperty("name",out var n)?n.GetString():null;
            result.Add(new WorldscapePlace {Id=id,Name=string.IsNullOrWhiteSpace(name)?"Unnamed "+(actual??category).ToLowerInvariant():name,
                Category=category=="All"?actual:category,Latitude=coordinates[1].GetDouble(),Longitude=coordinates[0].GetDouble(),
                Website=Read(raw,"website"),OpeningHours=Read(raw,"opening_hours"),Phone=Read(raw,"phone"),MenuUrl=Read(raw,"website:menu"),
                Source="Geoapify / OpenStreetMap",SourceUrl="https://www.geoapify.com/",FetchedUtc=DateTime.UtcNow});
        }
        return result.GroupBy(p=>p.Id).Select(g=>g.First()).ToArray();
    }
    private static string Read(JsonElement p,string name)=>p.TryGetProperty(name,out var x)?x.ToString():null;
}
