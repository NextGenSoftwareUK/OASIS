using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class AwsNumericSearchNativeTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeStoredNumbersKeepExactOrderingAndTextRemainsText(bool sync)
    {
        const string endpoint="http://127.0.0.1:18000";
        Assert.AreEqual(endpoint,Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(work,"aws-full-suite.pending.json")));
        Assert.AreEqual("verified-empty-before-exclusive-native-suite",intent.RootElement.GetProperty("state").GetString());
        var marker=Guid.NewGuid().ToString("D");
        static JsonElement Number(string text){using var document=JsonDocument.Parse(text);return document.RootElement.Clone();}
        var values=new object[]{Number("1.0000000000000000000000000000000001"),Number("0.9999999999999999999999999999999999"),Number("1e1000"),"1"};
        var rows=values.Select(value=>new Holon{Id=Guid.NewGuid(),MetaData=new(){{"numericFixture",marker},{"score",value}}}).ToArray();
        var provider=new AWSOASIS("us-east-1","OwnedFullSuite","owned-synthetic-secret",endpoint);
        using var native=new AmazonDynamoDBClient(new BasicAWSCredentials("OwnedFullSuite","owned-synthetic-secret"),new AmazonDynamoDBConfig{ServiceURL=endpoint,AuthenticationRegion="us-east-1",UseHttp=true,MaxErrorRetry=0});
        async Task<string> Read(Guid id){var observed=await native.GetItemAsync(new GetItemRequest{TableName="OasisHolons",ConsistentRead=true,Key=new(){{"id",new(){S=id.ToString()}}}});Assert.AreEqual(HttpStatusCode.OK,observed.HttpStatusCode);return observed.Item["data_json"].S;}
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var originals=new Dictionary<Guid,string>();
            foreach(var row in rows){var saved=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());originals.Add(row.Id,await Read(row.Id));}
            foreach(var operation in Enum.GetValues<SearchOperatorType>()){
                bool Match(int comparison)=>operation switch{
                    SearchOperatorType.LessThan=>comparison<0,SearchOperatorType.LessThanOrEqualTo=>comparison<=0,
                    SearchOperatorType.EqualTo=>comparison==0,SearchOperatorType.NotEqualTo=>comparison!=0,
                    SearchOperatorType.GreaterThan=>comparison>0,SearchOperatorType.GreaterThanOrEqualTo=>comparison>=0,
                    _=>throw new ArgumentOutOfRangeException(nameof(operation))};
                var expected=rows.Take(3).Where((row,index)=>Match(index==1?-1:1)).Select(row=>row.Id).Order().ToArray();
                var query=new SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){{"numericFixture",marker}},SearchGroups=new List<ISearchGroupBase>{
                    new SearchNumberGroup{SearchHolons=true,HolonType=HolonType.All,Number=1,NumberOperator=operation,
                        HolonSearchParams=new NextGenSoftware.OASIS.API.Core.Interfaces.SearchHolonParams{MetaData=true,MetaDataKey="score"}}}};
                var found=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
                Assert.IsFalse(found.IsError,found.Exception?.ToString());Assert.AreEqual(expected.Length,found.LoadedCount);
                CollectionAssert.AreEqual(expected,found.Result.SearchResultHolons.Select(row=>row.Id).Order().ToArray());
            }
            foreach(var row in rows)Assert.AreEqual(originals[row.Id],await Read(row.Id),"Search cannot rewrite exact stored numeric values.");
        }finally{await provider.DeActivateProviderAsync();}
        // The existing full-suite owner journals and conditionally removes all exact originals.
    }
}
