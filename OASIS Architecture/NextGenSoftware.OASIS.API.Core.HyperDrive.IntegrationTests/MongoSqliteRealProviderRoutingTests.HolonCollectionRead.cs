using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoCollectionGraphReadsAsync(MongoDBOASIS mongo,SQLLiteDBOASIS sqlite,OASISHyperDrive router,bool synchronous,IMongoCollection<NativeHolon> records)
    {
        var parent=new Holon {Id=Guid.NewGuid(),IsActive=true,HolonType=HolonType.Zome,Name="collection parent"};
        var root=new Holon {Id=Guid.NewGuid(),IsActive=true,Version=317,HolonType=HolonType.Quest,ParentHolonId=parent.Id,Name="selected collection root"};
        root.Description=root.Name;
        var child=new Holon {Id=Guid.NewGuid(),IsActive=true,Version=8,HolonType=HolonType.Holon,ParentHolonId=root.Id,Name="native descendant outside root filter"};
        var grand=new Holon {Id=Guid.NewGuid(),IsActive=true,Version=9,HolonType=HolonType.Holon,ParentHolonId=child.Id,Name="native grandchild"};
        var deleted=new Holon {Id=Guid.NewGuid(),IsActive=false,DeletedDate=DateTime.UtcNow,ParentHolonId=root.Id,Name="deleted child must be excluded"};
        var embeddedGrand=new Holon {Id=grand.Id,Version=9,ParentHolonId=child.Id,Name="embedded grandchild"};
        var embeddedChild=new Holon {Id=child.Id,Version=8,ParentHolonId=root.Id,Name="embedded child",Children=new List<IHolon>{embeddedGrand}};
        root.Children=new List<IHolon>{embeddedChild};
        var literal="$literal-"+root.Id.ToString("N");root.MetaData["NFT.MintedByAvatarId"]=literal;root.MetaData["Status"]="Ready";root.MetaData["$field"]=literal;
        var ids=new[] {parent.Id,root.Id,child.Id,grand.Id,deleted.Id};
        try
        {
            var savedParent=await mongo.SaveHolonAsync(parent,saveChildren:false);savedParent.IsError.Should().BeFalse(savedParent.Message);
            var key=savedParent.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
            foreach(var node in new[] {root,child,grand,deleted}) (await mongo.SaveHolonAsync(node,saveChildren:false)).IsError.Should().BeFalse();
            (bool Children,bool Recursive,int Depth,int Initial)[] options={
                (Children:false,Recursive:false,Depth:0,Initial:0),(true,false,0,0),(true,true,0,0),
                (true,true,1,0),(true,true,2,0),(true,true,2,1),(true,true,1,1)};
            foreach(var selection in new[] {"all","parent","parentKey","metadata","metadataAll","metadataAny"})
            foreach(var option in options)
            foreach(var fromProvider in new[] {false,true})
            {
                var operation=selection switch {"all"=>"LoadAllHolons","parent"=>"LoadHolonsForParent","parentKey"=>"LoadHolonsForParentByProviderKey","metadata"=>"LoadHolonsByMetaData",_=>"LoadHolonsByMetaDataPairs"};
                var request=new StorageOperationRequest {Operation=operation,HolonId=parent.Id,ProviderKey=key,HolonType=HolonType.Quest,
                    MetaKey="NFT.MintedByAvatarId",MetaValue=literal,MetaKeyValuePairs=new Dictionary<string,string>{{"NFT.MintedByAvatarId",literal},{"Status",selection=="metadataAny" ? "never":"Ready"}},
                    MetaKeyValuePairMatchMode=selection=="metadataAny" ? MetaKeyValuePairMatchMode.Any:MetaKeyValuePairMatchMode.All,
                    LoadChildren=option.Children,Recursive=option.Recursive,MaxChildDepth=option.Depth,CurrentChildDepth=option.Initial,
                    ChildrenFromProvider=fromProvider,Version=317,PreferredProvider=ProviderType.MongoDBOASIS};
                var result=synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request):await router.RouteRequestAsync<IEnumerable<IHolon>>(request);
                result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);result.IsLoaded.Should().BeTrue();
                var selected=result.Result.Should().ContainSingle().Which;selected.Id.Should().Be(root.Id);
                if(!option.Children || option.Depth!=0 && option.Initial>=option.Depth) selected.Children.Should().BeEmpty();
                else
                {
                    var projected=selected.Children.Should().ContainSingle().Which;
                    projected.Id.Should().Be(child.Id);projected.Name.Should().Be(fromProvider ? child.Name:embeddedChild.Name);
                    if(!option.Recursive || option.Depth!=0 && option.Initial+1>=option.Depth) projected.Children.Should().BeEmpty();
                    else projected.Children.Should().ContainSingle().Which.Name.Should().Be(fromProvider ? grand.Name:embeddedGrand.Name);
                }
            }
            var special=synchronous ? mongo.LoadHolonsByMetaData("$field",literal,HolonType.Quest,loadChildren:false,version:317)
                :await mongo.LoadHolonsByMetaDataAsync("$field",literal,HolonType.Quest,loadChildren:false,version:317);
            special.IsError.Should().BeFalse(special.Exception?.ToString() ?? special.Message);special.Result.Should().ContainSingle().Which.Id.Should().Be(root.Id);
            var noMatch=new Dictionary<string,string>{{"NFT.MintedByAvatarId",literal},{"Status","never"}};
            var all=synchronous ? mongo.LoadHolonsByMetaData(noMatch,MetaKeyValuePairMatchMode.All,HolonType.Quest,loadChildren:false,version:317)
                :await mongo.LoadHolonsByMetaDataAsync(noMatch,MetaKeyValuePairMatchMode.All,HolonType.Quest,loadChildren:false,version:317);
            all.IsError.Should().BeFalse();all.Result.Should().BeEmpty("ALL must not behave like the successful ANY query above");
            foreach(var mode in new[] {MetaKeyValuePairMatchMode.Any,MetaKeyValuePairMatchMode.All})
            {
                var searchRequest=new StorageOperationRequest {Operation="Search",PreferredProvider=ProviderType.MongoDBOASIS,
                    SearchParams=new Core.Objects.Search.SearchParams {SearchOnlyForCurrentAvatar=false,
                        FilterByMetaData=new Dictionary<string,string>{{"NFT.MintedByAvatarId",literal},{"Status","Ready"}},
                        MetaKeyValuePairMatchMode=mode,
                        SearchGroups=new() {new Core.Objects.Search.SearchTextGroup {SearchQuery=root.Name,SearchHolons=true,
                            HolonType=root.HolonType,HolonSearchParams=new SearchHolonParams {Name=true}}}}};
                var searched=synchronous ? router.RouteRequest<Core.Interfaces.Search.ISearchResults>(searchRequest)
                    :await router.RouteRequestAsync<Core.Interfaces.Search.ISearchResults>(searchRequest);
                searched.IsError.Should().BeFalse(searched.Exception?.ToString() ?? searched.Message);
                searched.Result.SearchResultHolons.Select(hit=>hit.Id).Should().Equal(new[] {root.Id},"multiple matching metadata keys must not duplicate a search hit");
            }
            foreach(var byName in new[] {true,false})
            foreach(var type in new[] {HolonType.All,HolonType.Quest,HolonType.Zome})
            foreach(var parentFilter in new[] {Guid.Empty,parent.Id,child.Id})
            {
                var request=new StorageOperationRequest {Operation="Search",PreferredProvider=ProviderType.MongoDBOASIS,
                    SearchParams=new Core.Objects.Search.SearchParams {SearchOnlyForCurrentAvatar=false,ParentId=parentFilter,
                        SearchGroups=new() {new Core.Objects.Search.SearchTextGroup {SearchQuery=root.Name.ToUpperInvariant(),SearchHolons=true,
                            HolonType=type,HolonSearchParams=new SearchHolonParams {Name=byName,Description=!byName}}}}};
                var found=synchronous ? router.RouteRequest<Core.Interfaces.Search.ISearchResults>(request)
                    :await router.RouteRequestAsync<Core.Interfaces.Search.ISearchResults>(request);
                found.IsError.Should().BeFalse(found.Exception?.ToString() ?? found.Message);
                var expected=type!=HolonType.Zome && parentFilter!=child.Id ? new[] {root.Id}:Array.Empty<Guid>();
                found.Result.SearchResultHolons.Select(hit=>hit.Id).Should().Equal(expected,"All is a wildcard and parent/type restrictions must still apply");
            }
            await VerifyMongoSearchExecutionAsync(mongo,router,synchronous,root);
            await VerifyMongoScalarMetadataAsync(mongo,sqlite,synchronous,records);
            var missing=synchronous ? mongo.LoadHolonsForParent(ObjectId.GenerateNewId().ToString(),loadChildren:true,loadChildrenFromProvider:true)
                :await mongo.LoadHolonsForParentAsync(ObjectId.GenerateNewId().ToString(),loadChildren:true,loadChildrenFromProvider:true);
            missing.IsError.Should().BeFalse();missing.IsLoaded.Should().BeTrue();missing.Result.Should().BeEmpty();
            var invalid=synchronous ? mongo.LoadAllHolons(curentChildDepth:-1):await mongo.LoadAllHolonsAsync(curentChildDepth:-1);
            invalid.IsError.Should().BeTrue();invalid.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
        }
        finally {await records.DeleteManyAsync(row=>ids.Contains(row.HolonId));}
    }
}
