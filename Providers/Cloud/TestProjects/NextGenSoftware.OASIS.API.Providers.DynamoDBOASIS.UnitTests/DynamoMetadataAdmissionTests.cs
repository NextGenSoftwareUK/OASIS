using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoMetadataAdmissionTests
{
    [DataTestMethod]
    [DataRow(null,false)][DataRow(null,true)][DataRow("",false)][DataRow("",true)][DataRow(" ",false)][DataRow(" ",true)]
    public async Task InvalidSingleMetadataKeyReturnsResultInsteadOfFaultedTask(string? key,bool synchronous)
    {
        var provider=new DynamoDBOASIS("synthetic","synthetic","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
        using var client=(Amazon.DynamoDBv2.AmazonDynamoDBClient)field.GetValue(provider)!;
        var result=synchronous?provider.LoadHolonsByMetaData(key!,"value"):await provider.LoadHolonsByMetaDataAsync(key!,"value");
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
        Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
    }
}
