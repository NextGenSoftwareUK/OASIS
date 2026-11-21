using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private static async Task<Guid> PublishActualOurWorldRequest(string address,string bearer,Guid actor)
    {
        // These are the exact pinned game sources linked by the workflow, not duplicated DTOs.
        var assembly=typeof(AvatarProfileCommitStampNativeTests).Assembly;
        var routeType=assembly.GetType("OASIS.GameIntegration.WalkingRoute",true)!;
        var pointType=assembly.GetType("OASIS.GameIntegration.WalkingRoutePoint",true)!;
        var publication=assembly.GetType("OASIS.GameIntegration.WalkingRoutePublication",true)!;
        var route=Activator.CreateInstance(routeType)!;var id=Guid.NewGuid();
        void Set(object target,string field,object value)=>target.GetType().GetField(field)!.SetValue(target,value);
        Set(route,"Id",id.ToString("D"));Set(route,"Name","Our World cloud walk");
        Set(route,"SourceApp","Our World");Set(route,"SourceRouteId","owned-http-world-route");
        var points=(System.Collections.IList)routeType.GetField("Points")!.GetValue(route)!;
        foreach(var sample in new[]{(lat:0d,lon:0d,start:true,utc:"2026-11-21T00:00:00.1234567Z"),
            (lat:0d,lon:0.001d,start:false,utc:(string?)null),(lat:1d,lon:1d,start:true,utc:(string?)null),
            (lat:1d,lon:1.001d,start:false,utc:(string?)null)})
        {
            var point=Activator.CreateInstance(pointType)!;Set(point,"Latitude",sample.lat);Set(point,"Longitude",sample.lon);
            Set(point,"StartsSegment",sample.start);if(sample.utc!=null)Set(point,"Utc",sample.utc);points.Add(point);
        }
        var operation=Guid.NewGuid();var intent=(JObject)publication.GetMethod("Create")!.Invoke(null,[route,operation])!;
        var body=intent.ToString(Formatting.None);
        Assert.DoesNotContain("karma",body,StringComparison.OrdinalIgnoreCase);
        using var http=new HttpClient{BaseAddress=new Uri(address),Timeout=TimeSpan.FromSeconds(30)};
        http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",bearer);
        using var create=new HttpRequestMessage(HttpMethod.Post,"/api/map/routes") { Content=new StringContent(body,Encoding.UTF8,"application/json") };
        create.Headers.Add("X-COSMIC-Operation-Id",operation.ToString("D"));
        using var created=await http.SendAsync(create);Assert.True(created.IsSuccessStatusCode,$"Create HTTP {(int)created.StatusCode}");
        var reply=(JObject)publication.GetMethod("Parse")!.Invoke(null,[await created.Content.ReadAsStringAsync()])!;
        Assert.False((bool?)reply["isError"]??true,(string?)reply["message"]);
        publication.GetMethod("VerifyAcknowledgement")!.Invoke(null,[reply["result"],intent]);
        using var read=await http.GetAsync("/api/map/routes/"+id.ToString("D"));Assert.True(read.IsSuccessStatusCode);
        var detail=(JObject)publication.GetMethod("Parse")!.Invoke(null,[await read.Content.ReadAsStringAsync()])!;
        Assert.False((bool?)detail["isError"]??true,(string?)detail["message"]);
        publication.GetMethod("VerifyDetail")!.Invoke(null,[detail["result"],intent,actor]);
        return id;
    }
}
