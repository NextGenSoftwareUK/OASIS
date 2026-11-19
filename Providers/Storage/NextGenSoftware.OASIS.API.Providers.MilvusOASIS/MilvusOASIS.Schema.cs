using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS;

public partial class MilvusOASIS
{
    private async Task EnsureCollectionAsync(string collection)
    {
        // Explicit provisioning: never turn a native error into replacement creation.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Post,$"{_baseUrl}/v2/vectordb/collections/describe")
            {Content=Json(new{collectionName=collection})};
        request.Headers.Add("Request-Timeout","15");
        using var response = await _http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var body = new MemoryStream();
        var buffer = new byte[8192]; int count;
        while ((count=await stream.ReadAsync(buffer,deadline.Token))!=0)
        {
            if(body.Length+count>262144)throw new InvalidDataException("Milvus schema exceeds256KiB.");
            body.Write(buffer,0,count);
        }
        using var document = JsonDocument.Parse(body.ToArray());
        if(document.RootElement.GetProperty("code").GetInt32()!=0)throw new InvalidDataException("Milvus collection description failed.");
        var data=document.RootElement.GetProperty("data");
        if(data.GetProperty("collectionName").GetString()!=collection || data.GetProperty("collectionID").GetInt64()<=0 ||
            data.GetProperty("autoId").GetBoolean() || data.GetProperty("enableDynamicField").GetBoolean() ||
            data.GetProperty("consistencyLevel").GetString()!="Strong" || data.GetProperty("load").GetString()!="LoadStateLoaded")
            throw new InvalidDataException("Milvus requires an explicitly provisioned loaded collection with Strong default consistency.");
        var fields=data.GetProperty("fields").EnumerateArray().ToDictionary(f=>f.GetProperty("name").GetString()!);
        if(fields.Count!=6)throw new InvalidDataException("Unexpected Milvus storage fields.");
        void Field(string name,string type,string parameter,string expected,bool primary=false)
        {
            if(!fields.TryGetValue(name,out var field) || field.GetProperty("type").GetString()!=type ||
                field.GetProperty("primaryKey").GetBoolean()!=primary || field.GetProperty("autoId").GetBoolean() ||
                field.GetProperty("partitionKey").GetBoolean())throw new InvalidDataException("Milvus field differs: "+name);
            var parameters=field.GetProperty("params").EnumerateArray().ToDictionary(v=>v.GetProperty("key").GetString()!,v=>v.GetProperty("value").GetString());
            if(!parameters.TryGetValue(parameter,out var value) || value!=expected)throw new InvalidDataException("Milvus field bound differs: "+name);
        }
        Field("id","VarChar","max_length","64",true);Field("doc","VarChar","max_length","65535");
        Field("username","VarChar","max_length","512");Field("email","VarChar","max_length","512");
        Field("parent_holon_id","VarChar","max_length","64");Field("vector","FloatVector","dim","2");
        var indexes=data.GetProperty("indexes").EnumerateArray().Where(i=>i.GetProperty("fieldName").GetString()=="vector").ToArray();
        if(indexes.Length!=1 || indexes[0].GetProperty("indexName").GetString()!="vector_idx" || indexes[0].GetProperty("metricType").GetString()!="L2")
            throw new InvalidDataException("Milvus vector index differs from storage contract.");
    }
}
