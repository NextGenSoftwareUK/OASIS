using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ConditionalReceiptRejectsReusedOperationWithChangedRoleIntent(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=new Holon{Id=Guid.NewGuid(),Name="role-receipt"};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="role-child"};root.Children.Add(child);
        var write=new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),ProviderType.SQLLiteDBOASIS,
            new[]{new HolonWriteCondition(root.Id,null),new HolonWriteCondition(child.Id,null)});
        ConditionalHolonWriteRequest Request(string role)=>new(write,ConditionalHolonOperation.Save,new[]{root},true,true,1,
            new Dictionary<Guid,HolonChildRoleManifest>{{root.Id,new HolonChildRoleManifest(1,root.Id,
                new[]{new HolonChildRole(role,new[]{child.Id})})}});
        var first=synchronous ? provider.ExecuteConditionalHolonWrite(Request("A"))
            :await provider.ExecuteConditionalHolonWriteAsync(Request("A"));
        first.IsError.Should().BeFalse(first.Message+first.Exception);
        var committedManifest=((Holon)first.Result.Single()).ChildRoleManifestJson;
        NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Deserialize(committedManifest).Roles.Single().Name.Should().Be("A");
        var replay=synchronous ? provider.ExecuteConditionalHolonWrite(Request("A"))
            :await provider.ExecuteConditionalHolonWriteAsync(Request("A"));
        replay.IsError.Should().BeFalse(replay.Message+replay.Exception);
        ((Holon)replay.Result.Single()).ChildRoleManifestJson.Should().Be(committedManifest);
        var conflict=synchronous ? provider.ExecuteConditionalHolonWrite(Request("B"))
            :await provider.ExecuteConditionalHolonWriteAsync(Request("B"));
        conflict.IsError.Should().BeTrue();conflict.ErrorCode.Should().Be("COSMIC_OPERATION_ID_REUSED");
        (await provider.LoadHolonAsync(root.Id)).Result.Version.Should().Be(first.Result.Single().Version);
        var loaded=(await provider.LoadHolonAsync(root.Id,loadChildren:false)).Result;
        ((Holon)loaded).ChildRoleManifestJson.Should().Be(committedManifest);
        root.Name="partial root update";root.ChildRoleManifestJson="caller cannot replace excluded stored roles";
        var partial=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),write.ActorId,ProviderType.SQLLiteDBOASIS,
            new[]{new HolonWriteCondition(root.Id,Revision(loaded))}),ConditionalHolonOperation.Save,new[]{root},false,false,0);
        var updated=await provider.ExecuteConditionalHolonWriteAsync(partial);
        updated.IsError.Should().BeFalse(updated.Message+updated.Exception);
        ((Holon)(await provider.LoadHolonAsync(root.Id,loadChildren:false)).Result).ChildRoleManifestJson.Should().Be(committedManifest);
        var historical=synchronous ? provider.LoadHolon(root.Id,loadChildren:false,version:1)
            :await provider.LoadHolonAsync(root.Id,loadChildren:false,version:1);
        historical.IsError.Should().BeFalse(historical.Message+historical.Exception);
        historical.Result.Name.Should().Be("role-receipt");historical.Result.Version.Should().Be(1);
        ((Holon)historical.Result).ChildRoleManifestJson.Should().Be(committedManifest);
        var beforeFailure=(await provider.LoadHolonAsync(root.Id,loadChildren:false)).Result;
        var childBeforeFailure=(await provider.LoadHolonAsync(child.Id,loadChildren:false)).Result;
        var failedOperation=Guid.NewGuid();
        var failedRequest=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(failedOperation,write.ActorId,ProviderType.SQLLiteDBOASIS,
            new[]{new HolonWriteCondition(root.Id,Revision(beforeFailure)),new HolonWriteCondition(child.Id,Revision(childBeforeFailure))}),
            ConditionalHolonOperation.Save,new[]{root},true,true,1,
            new Dictionary<Guid,HolonChildRoleManifest>{{root.Id,new HolonChildRoleManifest(1,root.Id,
                new[]{new HolonChildRole("B",new[]{child.Id})})}});
        using var control=new DataContext(TestConnectionString);
        control.Database.OpenConnection();
        using(var history=control.Database.GetDbConnection().CreateCommand())
        {
            history.CommandText="SELECT RecordJson,RecordDigest FROM CosmicHolonRevision WHERE HolonId=$id AND Version=1";
            var id=history.CreateParameter();id.ParameterName="$id";id.Value=root.Id.ToString("D");history.Parameters.Add(id);
            using var archived=history.ExecuteReader();archived.Read().Should().BeTrue();
            var json=archived.GetString(0);
            var previous=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Deserialize<IHolon>(json);
            previous.Version.Should().Be(1);((Holon)previous).ChildRoleManifestJson.Should().Be(committedManifest);
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json))).Should().Be(archived.GetString(1));
            archived.Read().Should().BeFalse();
        }
        control.Database.ExecuteSqlRaw($"CREATE TRIGGER reject_role_child BEFORE UPDATE ON Holon WHEN NEW.Id = '{child.Id:D}' BEGIN SELECT RAISE(ABORT, 'reject role child'); END");
        try
        {
            var failed=synchronous ? provider.ExecuteConditionalHolonWrite(failedRequest)
                :await provider.ExecuteConditionalHolonWriteAsync(failedRequest);
            failed.IsError.Should().BeTrue();
        }
        finally { control.Database.ExecuteSqlRaw("DROP TRIGGER reject_role_child"); }
        var afterFailure=(await provider.LoadHolonAsync(root.Id,loadChildren:false)).Result;
        ((Holon)afterFailure).ChildRoleManifestJson.Should().Be(committedManifest);
        afterFailure.Version.Should().Be(beforeFailure.Version);
        (await provider.LoadHolonAsync(child.Id,loadChildren:false)).Result.Version.Should().Be(childBeforeFailure.Version);
        using var receipt=control.Database.GetDbConnection().CreateCommand();
        receipt.CommandText="SELECT COUNT(*) FROM CosmicWriteReceipt WHERE OperationId = $id";
        var parameter=receipt.CreateParameter();parameter.ParameterName="$id";parameter.Value=failedOperation.ToString("D");receipt.Parameters.Add(parameter);
        Convert.ToInt64(receipt.ExecuteScalar()).Should().Be(0);
        receipt.Parameters.Clear();
        receipt.CommandText="SELECT COUNT(*) FROM CosmicHolonRevision WHERE HolonId=$id AND Version=$version";
        var archiveId=receipt.CreateParameter();archiveId.ParameterName="$id";archiveId.Value=root.Id.ToString("D");receipt.Parameters.Add(archiveId);
        var archiveVersion=receipt.CreateParameter();archiveVersion.ParameterName="$version";archiveVersion.Value=beforeFailure.Version;receipt.Parameters.Add(archiveVersion);
        Convert.ToInt64(receipt.ExecuteScalar()).Should().Be(0,"a failed graph transaction must roll back its newly captured revision");
        control.Database.ExecuteSqlRaw("UPDATE CosmicHolonRevision SET RecordDigest = {0} WHERE HolonId = {1} AND Version = 1",
            new string('0',64),root.Id.ToString("D"));
        var corruptHistory=synchronous ? provider.LoadHolon(root.Id,loadChildren:false,version:1)
            :await provider.LoadHolonAsync(root.Id,loadChildren:false,version:1);
        corruptHistory.IsError.Should().BeTrue("archived content must match its checksum");
        corruptHistory.Result.Should().BeNull();corruptHistory.Exception.Should().BeOfType<System.IO.InvalidDataException>();
        var row=control.Holons.AsNoTracking().Single(record=>record.Id==root.Id.ToString());
        var extension=Newtonsoft.Json.Linq.JObject.Parse(row.PersistentValueJson);
        extension[nameof(Holon.ChildRoleManifestJson)]=NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Serialize(
            new HolonChildRoleManifest(1,Guid.NewGuid(),new[]{new HolonChildRole("A",new[]{child.Id})}));
        control.Database.ExecuteSqlRaw("UPDATE Holon SET PersistentValueJson = {0} WHERE Id = {1}",
            extension.ToString(Newtonsoft.Json.Formatting.None),root.Id.ToString());
        var corrupt=synchronous ? provider.LoadHolon(root.Id,loadChildren:false):await provider.LoadHolonAsync(root.Id,loadChildren:false);
        corrupt.IsError.Should().BeTrue("stored relationship parent must agree with the native record identity");
        corrupt.Result.Should().BeNull();corrupt.Exception.Should().NotBeNull();
    }
}
