using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class RealProviderRoutingTests
{
    private sealed class ObservedImportSqlite(string connection,Func<bool> unpublished) : SQLLiteDBOASIS(connection)
    {
        public bool Before;public bool After;
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> records)
        {
            Before=unpublished();var result=await base.ImportAsync(records);After=unpublished();return result;
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ImportPublishesCallerGraphOnlyAfterRealSqliteAcknowledgement(bool synchronous)
    {
        Directory.CreateDirectory(_root);
        var source=new Holon {Name="owned staged root",IsActive=true};var child=new Holon {Name="owned staged child",IsActive=true};source.Children.Add(child);
        using var sqlite=new ObservedImportSqlite($"Data Source={SqlitePath};Pooling=False",()=>source.Id==Guid.Empty && child.Id==Guid.Empty && child.ParentHolonId==Guid.Empty);
        var manager=CreateManager();manager.RegisterProvider(sqlite);var router=new OASISHyperDrive(manager);
        var request=new StorageOperationRequest {Operation="Import",Payload=new IHolon[]{source},PreferredProvider=ProviderType.SQLLiteDBOASIS};
        var result=synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request);
        result.IsError.Should().BeFalse(result.Message);result.Result.Should().BeTrue();sqlite.Before.Should().BeTrue();sqlite.After.Should().BeTrue();
        source.Id.Should().NotBeEmpty();child.Id.Should().NotBeEmpty();child.ParentHolonId.Should().Be(source.Id);
        var native=await sqlite.LoadHolonAsync(source.Id,loadChildrenFromProvider:true);native.IsError.Should().BeFalse(native.Message);
        native.Result.Name.Should().Be(source.Name);native.Result.Children.Single().Id.Should().Be(child.Id);
        (await sqlite.LoadHolonAsync(child.Id,loadChildren:false)).Result.ParentHolonId.Should().Be(source.Id);
        source.Children.Single().Should().BeSameAs(child);
    }
}
