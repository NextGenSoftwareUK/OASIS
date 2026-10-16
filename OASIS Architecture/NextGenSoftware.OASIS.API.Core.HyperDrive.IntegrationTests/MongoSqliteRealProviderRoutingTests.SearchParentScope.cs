using FluentAssertions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoSearchParentScopeAsync(MongoDBOASIS mongo,NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS sqlite,bool synchronous)
    {
        var text="owned-parent-search-"+Guid.NewGuid().ToString("N");var when=new DateTime(2019,2,3,0,0,0,DateTimeKind.Utc);
        Holon Create(HolonType type)=>new() {Id=Guid.NewGuid(),Name=text,Version=7272,CreatedDate=when,HolonType=type};
        var parent=Create(HolonType.Park);var child=Create(HolonType.Quest);var grand=Create(HolonType.Park);var outside=Create(HolonType.Park);
        child.ParentHolonId=parent.Id;grand.ParentHolonId=child.Id;
        var records=new[] {parent,child,grand,outside};var ids=records.Select(row=>row.Id).ToArray();
        var avatar=new Avatar {Id=Guid.NewGuid(),Username=text,Version=7272,CreatedDate=when,
            AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)};
        var direct=new Avatar {Id=Guid.NewGuid(),Username=text+"direct",ParentHolonId=parent.Id,Version=7272,CreatedDate=when,AvatarType=avatar.AvatarType};
        var linked=new Avatar {Id=Guid.NewGuid(),Username=text+"linked",ParentHolonId=child.Id,Version=7272,CreatedDate=when,AvatarType=avatar.AvatarType};
        var avatarIds=new[] {avatar.Id,direct.Id,linked.Id};
        var nativeKeys=Array.Empty<string>();
        try
        {
            foreach(var record in records) (await mongo.SaveHolonAsync(record,saveChildren:false)).IsError.Should().BeFalse();
            nativeKeys=(await mongo.Database.Holon.Find(row=>ids.Contains(row.HolonId)).Project(row=>row.Id).ToListAsync()).ToArray();
            (await mongo.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
            foreach(var record in new[] {direct,linked})
            {
                (await mongo.SaveAvatarAsync(record)).IsError.Should().BeFalse();
                (await mongo.LoadAvatarAsync(record.Id)).Result.ParentHolonId.Should().Be(record.ParentHolonId);
            }
            foreach(var recursive in new[] {false,true})
            foreach(var type in new[] {HolonType.All,HolonType.Park})
            foreach(var kind in new[] {"text","number","date"})
            {
                ISearchGroupBase group=kind=="text" ? new SearchTextGroup {SearchQuery=text}:
                    kind=="number" ? new SearchNumberGroup {Number=7272,NumberOperator=SearchOperatorType.EqualTo}:
                    new SearchDateGroup {Date=when,DateOperator=SearchOperatorType.EqualTo};
                group.SearchHolons=true;group.SearchAvatars=true;group.HolonType=type;
                group.HolonSearchParams=new SearchHolonParams {Name=kind=="text",Version=kind=="number",CreatedDate=kind=="date"};
                group.AvatarSearchParams=new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams {Username=kind=="text",Version=kind=="number",CreatedDate=kind=="date"};
                var query=new SearchParams {SearchOnlyForCurrentAvatar=false,ParentId=parent.Id,Recursive=recursive,SearchGroups=new() {group}};
                var result=synchronous ? mongo.Search(query,loadChildren:false):await mongo.SearchAsync(query,loadChildren:false);
                result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);
                var expected=(recursive ? new[] {child,grand}:new[] {child}).Where(row=>type==HolonType.All || row.HolonType==type).Select(row=>row.Id);
                result.Result.SearchResultHolons.Select(row=>row.Id).Should().BeEquivalentTo(expected,"kind={0}, recursive={1}, type={2}",kind,recursive,type);
                var expectedAvatars=type==HolonType.All ? recursive ? new[] {direct.Id,linked.Id}:new[] {direct.Id}:Array.Empty<Guid>();
                result.Result.SearchResultAvatars.Select(row=>row.Id).Should().BeEquivalentTo(expectedAvatars);
            }
            (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await sqlite.SaveHolonAsync(child,saveChildren:false)).IsError.Should().BeFalse();
            var scoped=new SearchParams {SearchOnlyForCurrentAvatar=false,ParentId=parent.Id,Recursive=true,SearchGroups=new() {
                new SearchTextGroup {SearchQuery=text,SearchHolons=true,HolonType=HolonType.All,HolonSearchParams=new SearchHolonParams {Name=true}}}};
            var backup=await sqlite.SearchAsync(scoped,loadChildren:false);backup.IsError.Should().BeFalse(backup.Message);
            backup.Result.SearchResultHolons.Select(row=>row.Id).Should().Contain(child.Id);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true};manager.RegisterProvider(mongo);manager.RegisterProvider(sqlite);
            manager.SetAndReplaceAutoFailOverListForProviders(new[] {new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
                new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError.Should().BeFalse();
            var router=new Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var grandKey=await mongo.Database.Holon.Find(row=>row.HolonId==grand.Id).Project(row=>row.Id).SingleAsync();
            foreach(var fault in new[] {"cycle","empty-identity"})
            {
                if(fault=="cycle")
                    (await mongo.Database.Holon.UpdateOneAsync(row=>row.HolonId==parent.Id,
                        Builders<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon>.Update.Set(row=>row.ParentHolonId,grand.Id))).MatchedCount.Should().Be(1);
                else
                    (await mongo.Database.Holon.UpdateOneAsync(row=>row.Id==grandKey,
                        Builders<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon>.Update.Set(row=>row.HolonId,Guid.Empty))).MatchedCount.Should().Be(1);
                try
                {
                    var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests??0;
                    var request=new StorageOperationRequest {Operation="Search",SearchParams=scoped,LoadChildren=false,PreferredProvider=ProviderType.MongoDBOASIS};
                    var rejected=synchronous ? router.RouteRequest<ISearchResults>(request):await router.RouteRequestAsync<ISearchResults>(request);
                    rejected.IsError.Should().BeTrue();rejected.IsLoaded.Should().BeFalse();rejected.Result.Should().BeNull();
                    rejected.Exception.Should().BeOfType<InvalidDataException>();
                    (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests??0).Should().Be(before);
                }
                finally
                {
                    if(fault=="cycle") await mongo.Database.Holon.UpdateOneAsync(row=>row.HolonId==parent.Id,
                        Builders<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon>.Update.Set(row=>row.ParentHolonId,Guid.Empty));
                    else await mongo.Database.Holon.UpdateOneAsync(row=>row.Id==grandKey,
                        Builders<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon>.Update.Set(row=>row.HolonId,grand.Id));
                }
            }
            (await sqlite.DeleteHolonAsync(child.Id)).IsError.Should().BeFalse();
        }
        finally
        {
            await mongo.Database.Holon.DeleteManyAsync(row=>ids.Contains(row.HolonId)||nativeKeys.Contains(row.Id));
            await mongo.Database.Avatar.DeleteManyAsync(row=>avatarIds.Contains(row.HolonId));
        }
    }
}
