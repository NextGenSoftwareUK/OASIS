using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public static IEnumerable<object[]> LifecycleCases()
    {foreach(var history in new[]{false,true})foreach(var sync in new[]{false,true})foreach(var scenario in new[]{"active","cleared","retimed","reattributed","compatible","live"})yield return new object[]{history,sync,scenario};}
    [Theory,MemberData(nameof(LifecycleCases))]
    public async Task ConditionalSaveChecksCommittedTombstoneBeforeRevisionHistoryOrReceipt(bool history,bool synchronous,string scenario)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=history};(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor=Guid.NewGuid();var source=new Holon{Id=Guid.NewGuid(),Name="owned sqlite lifecycle"};
        var created=provider.ExecuteConditionalHolonWrite(ConditionalRequest(source,null,actor:actor));created.IsError.Should().BeFalse(created.Message);
        var current=created.Result.Single();
        if(scenario!="live")
        {
            var soft=provider.ExecuteConditionalHolonWrite(new(new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.SQLLiteDBOASIS,new[]{new HolonWriteCondition(current.Id,Revision(current))}),ConditionalHolonOperation.SoftDelete,new[]{current},saveChildren:false));
            soft.IsError.Should().BeFalse(soft.Message);current=soft.Result.Single();
        }
        string Native(){using var c=new DataContext(TestConnectionString);return JsonConvert.SerializeObject(c.Holons.AsNoTracking().Single());}
        var before=Native();var candidate=OasisJson.Project<Holon>(current);candidate.Name="owned sqlite lifecycle edit";
        if(scenario=="active"){candidate.IsActive=true;candidate.DeletedDate=DateTime.MinValue;candidate.DeletedByAvatarId=Guid.Empty;}
        if(scenario=="cleared")candidate.DeletedDate=DateTime.MinValue;
        if(scenario=="retimed")candidate.DeletedDate=candidate.DeletedDate.AddTicks(1);
        if(scenario=="reattributed")candidate.DeletedByAvatarId=Guid.NewGuid();
        var original=OasisJson.Serialize(candidate);var request=ConditionalRequest(candidate,Revision(current),actor:actor);
        var result=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        var after=Native();var evidence=Path.Combine(Path.GetDirectoryName(_databasePath)!,"lifecycle-"+history+"-"+synchronous+"-"+scenario+".json");
        File.WriteAllText(evidence,JsonConvert.SerializeObject(new{history,synchronous,scenario,operation=request.Write.OperationId,result.IsError,result.ErrorCode,before,after}));
        if(scenario is "compatible" or "live"){result.IsError.Should().BeFalse(result.Message+result.Exception);result.Result.Single().Name.Should().Be(candidate.Name);return;}
        result.IsError.Should().BeTrue();result.ErrorCode.Should().Be(StorageLifecycleConflictException.ErrorCode);result.IsSaved.Should().BeFalse();
        NativeMutationReconciliation.IsRequired(result.Exception).Should().BeFalse();after.Should().Be(before);OasisJson.Serialize(candidate).Should().Be(original);
        result.MetaData.Should().NotContainKey(HolonCommittedDelta.ResultMetadataKey);
        using var control=new DataContext(TestConnectionString);control.Database.OpenConnection();using var receipt=control.Database.GetDbConnection().CreateCommand();
        receipt.CommandText="SELECT COUNT(*) FROM CosmicWriteReceipt WHERE OperationId=$id";var id=receipt.CreateParameter();id.ParameterName="$id";id.Value=request.Write.OperationId.ToString();receipt.Parameters.Add(id);Convert.ToInt32(receipt.ExecuteScalar()).Should().Be(0);
    }
    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task OrdinarySaveCannotResurrectStoredTombstone(bool history,bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=history};(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id=Guid.NewGuid();var created=provider.ExecuteConditionalHolonWrite(ConditionalRequest(new Holon{Id=id,Name="owned ordinary lifecycle"},null));created.IsError.Should().BeFalse();
        var row=created.Result.Single();var deleted=provider.ExecuteConditionalHolonWrite(new(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),ProviderType.SQLLiteDBOASIS,new[]{new HolonWriteCondition(id,Revision(row))}),ConditionalHolonOperation.SoftDelete,new[]{row},saveChildren:false));deleted.IsError.Should().BeFalse();
        string Native(){using var context=new DataContext(TestConnectionString);return JsonConvert.SerializeObject(context.Holons.AsNoTracking().Single());}
        var before=Native();var candidate=new Holon{Id=id,Name="owned invalid ordinary resurrection"};var caller=OasisJson.Serialize(candidate);
        var failed=synchronous?provider.SaveHolon(candidate,saveChildren:false):await provider.SaveHolonAsync(candidate,saveChildren:false);
        failed.IsError.Should().BeTrue();failed.ErrorCode.Should().Be(StorageLifecycleConflictException.ErrorCode);NativeMutationReconciliation.IsRequired(failed.Exception).Should().BeFalse();
        Native().Should().Be(before);OasisJson.Serialize(candidate).Should().Be(caller);
    }

    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task LateChildLifecycleRejectionPrecedesEverySelectedHistoryAndReceipt(bool history,bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=history};(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=new Holon{Id=Guid.NewGuid(),Name="owned root before late rejection"};var child=new Holon{Id=Guid.NewGuid(),Name="owned late tombstone"};root.Children.Add(child);
        var created=provider.ExecuteConditionalHolonWrite(ConditionalRequest(root,null,conditions:new[]{new HolonWriteCondition(root.Id,null),new HolonWriteCondition(child.Id,null)}));created.IsError.Should().BeFalse(created.Message);
        var storedRoot=created.Result.Single();var storedChild=storedRoot.Children.Single();
        var soft=provider.ExecuteConditionalHolonWrite(new(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),ProviderType.SQLLiteDBOASIS,new[]{new HolonWriteCondition(child.Id,Revision(storedChild))}),ConditionalHolonOperation.SoftDelete,new[]{storedChild},saveChildren:false));soft.IsError.Should().BeFalse(soft.Message);
        string Native(){using var c=new DataContext(TestConnectionString);return JsonConvert.SerializeObject(c.Holons.AsNoTracking().OrderBy(row=>row.Id).ToArray());}
        long Count(string table){using var c=new DataContext(TestConnectionString);c.Database.OpenConnection();using var command=c.Database.GetDbConnection().CreateCommand();command.CommandText="SELECT COUNT(*) FROM "+table;return Convert.ToInt64(command.ExecuteScalar());}
        var before=Native();var archives=Count("CosmicHolonRevision");var receipts=Count("CosmicWriteReceipt");
        var candidate=OasisJson.Project<Holon>(storedRoot);candidate.Name="must not archive or persist";var invalid=OasisJson.Project<Holon>(soft.Result.Single());invalid.IsActive=true;invalid.DeletedDate=DateTime.MinValue;candidate.Children=new List<IHolon>{invalid};
        var request=ConditionalRequest(candidate,Revision(storedRoot),conditions:new[]{new HolonWriteCondition(root.Id,Revision(storedRoot)),new HolonWriteCondition(child.Id,Revision(soft.Result.Single()))});
        var result=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        result.IsError.Should().BeTrue();result.ErrorCode.Should().Be(StorageLifecycleConflictException.ErrorCode);Native().Should().Be(before);Count("CosmicHolonRevision").Should().Be(archives);Count("CosmicWriteReceipt").Should().Be(receipts);
    }

}
