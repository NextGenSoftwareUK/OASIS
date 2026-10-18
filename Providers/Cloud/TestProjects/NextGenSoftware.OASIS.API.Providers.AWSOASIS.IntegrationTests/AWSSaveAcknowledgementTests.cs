using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    private sealed class LostPutAcknowledgementClient : AmazonDynamoDBClient
    {
        public required AmazonDynamoDBClient Healthy;
        public readonly IOException Cause=new("Controlled loss after actual SDK PutItem acknowledgement.");
        public int Commits;
        public int FailOnCommit=1;
        public bool MissingResponse;
        public LostPutAcknowledgementClient():base(new BasicAWSCredentials("fakeMyKeyId","fakeSecretAccessKey"),
            new AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",UseHttp=true}) { }
        public override async Task<PutItemResponse> PutItemAsync(PutItemRequest request,CancellationToken cancellationToken=default)
        {
            var response=await Healthy.PutItemAsync(request,cancellationToken);
            Assert.AreEqual(System.Net.HttpStatusCode.OK,response.HttpStatusCode);
            Commits++;if(Commits==FailOnCommit){if(MissingResponse)return null!;throw Cause;}
            return response;
        }
    }

    [DataTestMethod]
    [DataRow(0,false,false)] [DataRow(0,true,false)]
    [DataRow(1,false,false)] [DataRow(1,true,false)]
    [DataRow(2,false,false)] [DataRow(2,true,false)]
    [DataRow(0,false,true)] [DataRow(0,true,true)]
    [DataRow(1,false,true)] [DataRow(1,true,true)]
    [DataRow(2,false,true)] [DataRow(2,true,true)]
    public async Task CommittedSaveWithLostAcknowledgementDoesNotPublishCallerIdentity(int kind,bool synchronous,bool missing)
    {
        var healthy=Client;
        using var fault=new LostPutAcknowledgementClient {Healthy=healthy,MissingResponse=missing};
        var profile=new Avatar {Username="owned-ack-profile",IsActive=true};
        var detail=new AvatarDetail {Username="owned-ack-detail",IsActive=true,XP=321};
        var holon=new Holon {Name="owned-ack-holon",IsActive=true};
        Exception cause;bool failed,saved;
        ClientField.SetValue(_provider,fault);
        try
        {
            if(kind==0)
            {
                var result=synchronous ? _provider.SaveAvatar(profile):await _provider.SaveAvatarAsync(profile);
                cause=result.Exception;failed=result.IsError;saved=result.IsSaved;
            }
            else if(kind==1)
            {
                var result=synchronous ? _provider.SaveAvatarDetail(detail):await _provider.SaveAvatarDetailAsync(detail);
                cause=result.Exception;failed=result.IsError;saved=result.IsSaved;
            }
            else
            {
                var result=synchronous ? _provider.SaveHolon(holon,saveChildren:false):await _provider.SaveHolonAsync(holon,saveChildren:false);
                cause=result.Exception;failed=result.IsError;saved=result.IsSaved;
            }
        }
        finally {ClientField.SetValue(_provider,healthy);}
        Assert.IsTrue(failed);Assert.IsFalse(saved);Assert.AreEqual(1,fault.Commits);
        Assert.IsInstanceOfType(cause,typeof(DynamoDBOASIS.DynamoDBMutationReconciliationException));
        if(missing)Assert.IsInstanceOfType<InvalidDataException>(cause.GetBaseException());else Assert.AreSame(fault.Cause,cause.GetBaseException());
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(cause));
        var evidence=(DynamoDBOASIS.DynamoDBMutationReconciliationException)cause;
        var table=kind==0 ? "OasisAvatars":kind==1 ? "OasisAvatarDetails":"OasisHolons";
        Assert.AreEqual(table,evidence.TableName);Assert.IsTrue(Guid.TryParse(evidence.RecordKey,out var attempted));
        Assert.AreNotEqual(Guid.Empty,attempted);
        var persisted=await healthy.GetItemAsync(new GetItemRequest {TableName=table,Key=Key(attempted),ConsistentRead=true});
        Assert.AreEqual(attempted.ToString(),persisted.Item["id"].S);
        Assert.AreEqual(Guid.Empty,profile.Id);Assert.AreEqual(Guid.Empty,detail.Id);Assert.AreEqual(Guid.Empty,holon.Id);
        Assert.IsFalse(profile.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.AWSOASIS));
        Assert.IsFalse(holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.AWSOASIS));
        Assert.IsFalse(detail.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.AWSOASIS));
        await healthy.DeleteItemAsync(new DeleteItemRequest {TableName=table,Key=Key(attempted)});
    }
}
