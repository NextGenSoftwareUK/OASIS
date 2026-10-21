using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoActivationSchemaTests
{
    private sealed class SchemaClient(int kind):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",MaxErrorRetry=0})
    {
        public override Task<DescribeTableResponse> DescribeTableAsync(string name,CancellationToken cancellationToken=default)
        {
            var keys=new List<KeySchemaElement>{new(){AttributeName=kind==1?"wrong":"id",KeyType=KeyType.HASH}};
            if(kind==2)keys.Add(new(){AttributeName="extra",KeyType=KeyType.RANGE});
            return Task.FromResult(new DescribeTableResponse{Table=new(){TableName=name,TableStatus=TableStatus.ACTIVE,KeySchema=keys,
                AttributeDefinitions=new(){new(){AttributeName="id",AttributeType=kind==3?ScalarAttributeType.N:ScalarAttributeType.S}}}});
        }
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
    public async Task ActivationRequiresCanonicalStringIdentityKey(int kind)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new SchemaClient(kind);field.SetValue(provider,client);
        try{
            var result=await provider.ActivateProviderAsync();
            Assert.AreEqual(kind!=0,result.IsError,"An ACTIVE table with an incompatible key must not activate.");
            Assert.AreEqual(kind==0,provider.IsProviderActivated);
            if(kind!=0)Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
