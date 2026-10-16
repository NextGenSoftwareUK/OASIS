using FluentAssertions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoSearchParentScopeAsync(MongoDBOASIS mongo,bool synchronous)
    {
        var text="owned-parent-search-"+Guid.NewGuid().ToString("N");var when=new DateTime(2019,2,3,0,0,0,DateTimeKind.Utc);
        Holon Create(HolonType type)=>new() {Id=Guid.NewGuid(),Name=text,Version=7272,CreatedDate=when,HolonType=type};
        var parent=Create(HolonType.Park);var child=Create(HolonType.Quest);var grand=Create(HolonType.Park);var outside=Create(HolonType.Park);
        child.ParentHolonId=parent.Id;grand.ParentHolonId=child.Id;
        var records=new[] {parent,child,grand,outside};var ids=records.Select(row=>row.Id).ToArray();
        try
        {
            foreach(var record in records) (await mongo.SaveHolonAsync(record,saveChildren:false)).IsError.Should().BeFalse();
            foreach(var recursive in new[] {false,true})
            foreach(var type in new[] {HolonType.All,HolonType.Park})
            foreach(var kind in new[] {"text","number","date"})
            {
                ISearchGroupBase group=kind=="text" ? new SearchTextGroup {SearchQuery=text}:
                    kind=="number" ? new SearchNumberGroup {Number=7272,NumberOperator=SearchOperatorType.EqualTo}:
                    new SearchDateGroup {Date=when,DateOperator=SearchOperatorType.EqualTo};
                group.SearchHolons=true;group.HolonType=type;
                group.HolonSearchParams=new SearchHolonParams {Name=kind=="text",Version=kind=="number",CreatedDate=kind=="date"};
                var query=new SearchParams {SearchOnlyForCurrentAvatar=false,ParentId=parent.Id,Recursive=recursive,SearchGroups=new() {group}};
                var result=synchronous ? mongo.Search(query,loadChildren:false):await mongo.SearchAsync(query,loadChildren:false);
                result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);
                var expected=(recursive ? new[] {child,grand}:new[] {child}).Where(row=>type==HolonType.All || row.HolonType==type).Select(row=>row.Id);
                result.Result.SearchResultHolons.Select(row=>row.Id).Should().BeEquivalentTo(expected,"kind={0}, recursive={1}, type={2}",kind,recursive,type);
            }
        }
        finally {await mongo.Database.Holon.DeleteManyAsync(row=>ids.Contains(row.HolonId));}
    }
}
