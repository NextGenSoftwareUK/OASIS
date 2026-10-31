using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    private sealed class RejectFirstGraphWriteClient : AmazonDynamoDBClient
    {
        public required AmazonDynamoDBClient Healthy;
        public int Attempts;
        public string RejectedKey="";
        public RejectFirstGraphWriteClient():base(new BasicAWSCredentials("fakeMyKeyId","fakeSecretAccessKey"),
            new AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",UseHttp=true,MaxErrorRetry=0}) { }
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
            => Healthy.GetItemAsync(request,cancellationToken);
        public override Task<PutItemResponse> PutItemAsync(PutItemRequest request,CancellationToken cancellationToken=default)
        {
            if(Attempts++==0)
            {
                RejectedKey=request.Item["id"].S;
                request.ConditionExpression="attribute_exists(id)"; // Actual server rejection for this absent synthetic key.
                request.ExpressionAttributeNames.Clear(); // Replaced predicate must not retain now-unused canonical aliases.
            }
            return Healthy.PutItemAsync(request,cancellationToken);
        }
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(false,true)]
    [DataRow(true,false)] [DataRow(true,true)]
    public async Task NativeGraphDefiniteRejectionSkipsDescendantsAndHonorsContinue(bool synchronous,bool keepGoing)
    {
        var healthy=Client;
        using var fault=new RejectFirstGraphWriteClient {Healthy=healthy};
        var rejected=new Holon {Name="owned-rejected-parent",IsActive=true};
        var skipped=new Holon {Name="owned-skipped-child",IsActive=true};
        var accepted=new Holon {Name="owned-independent-parent",IsActive=true};
        var acceptedChild=new Holon {Name="owned-independent-child",IsActive=true};
        rejected.Children=new List<IHolon> {skipped};accepted.Children=new List<IHolon> {acceptedChild};
        NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>> result;
        ClientField.SetValue(_provider,fault);
        try
        {
            var roots=new[] {rejected,accepted};
            result=synchronous ? _provider.SaveHolons(roots,continueOnError:keepGoing):await _provider.SaveHolonsAsync(roots,continueOnError:keepGoing);
        }
        finally {ClientField.SetValue(_provider,healthy);}
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(keepGoing ? 3:1,fault.Attempts);
        Assert.AreEqual(Guid.Empty,rejected.Id);Assert.AreEqual(Guid.Empty,skipped.Id);Assert.AreEqual(Guid.Empty,skipped.ParentHolonId);
        var rejectedId=Guid.Parse(fault.RejectedKey);
        Assert.AreEqual(0,(await healthy.GetItemAsync(new GetItemRequest {TableName="OasisHolons",Key=Key(rejectedId),ConsistentRead=true})).Item.Count);
        Assert.AreEqual(0,(await _provider.LoadHolonsForParentAsync(rejectedId,loadChildren:false)).Result.Count());
        if(keepGoing)
        {
            Assert.AreNotEqual(Guid.Empty,accepted.Id);Assert.AreNotEqual(Guid.Empty,acceptedChild.Id);
            Assert.AreEqual(accepted.Id,acceptedChild.ParentHolonId);
            var partial=(DynamoDBOASIS.DynamoDBPartialMutationException)result.Exception;
            CollectionAssert.AreEquivalent(new[] {accepted.Id,acceptedChild.Id},partial.AcknowledgedRecordIds.ToArray());
            var graph=await _provider.LoadHolonAsync(accepted.Id,loadChildrenFromProvider:true);
            Assert.IsFalse(graph.IsError,graph.Message);Assert.AreEqual(acceptedChild.Id,graph.Result.Children.Single().Id);
            foreach(var node in new[] {accepted,acceptedChild}) await healthy.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=Key(node.Id)});
        }
        else
        {
            Assert.IsInstanceOfType(result.Exception,typeof(ConditionalCheckFailedException));
            Assert.AreEqual(Guid.Empty,accepted.Id);Assert.AreEqual(Guid.Empty,acceptedChild.Id);
        }
    }
}
