using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace OASIS.ProviderTests.Fixtures;

// One source-linked contract fixture is used by all three real provider suites.
public sealed class CqlRoleItem:Holon
{
    [CustomOASISProperty]
    public string Label { get; set; }="";
}

public sealed class CqlRoleOwner:Holon
{
    public List<CqlRoleItem> Extras { get; set; }=new();
    public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Extras).ToList().AsReadOnly();
    public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()
        =>base.CaptureChildCollections().Concat(new[]{new HolonChildCollection("Extras",Extras,
            members=>Extras=members.Cast<CqlRoleItem>().ToList())}).ToArray();
    public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings()
        =>new[]{HolonChildRoleBinding.Create<IHolon>("Children",members=>Children=members.ToList()),
            HolonChildRoleBinding.Create<CqlRoleItem>("Extras",members=>Extras=members.ToList(),source=>
            {
                var item=StoredRecordJson.Project<CqlRoleItem>(source);
                item.Label=source.MetaData["Label"].ToString()!;
                return item;
            })};
}

public static class CqlCustomRoleGraph
{
    public static Holon CreateRoot()=>new CqlRoleOwner{Id=Guid.NewGuid(),Name="owned CQL graph root"};

    public static void Configure(Holon source)
    {
        var owner=(CqlRoleOwner)source;
        var extra=StoredRecordJson.Project<CqlRoleItem>(owner.Children[1]);
        extra.Label="stored typed extra";extra.MetaData["Label"]=extra.Label;
        owner.Children.RemoveAt(1);owner.Extras.Add(extra);
        owner.IsPublic=true;owner.Children[0].IsPublic=true;extra.IsPublic=false;
    }

    public static async Task VerifyAsync(HolonManager manager,ICosmicQueryStorageProvider adapter,
        Holon source,bool accessBound)
    {
        var id=source.Id;
        var query=CosmicQuery<CqlRoleOwner>.Create().Where(row=>row.Id==id)
            .WithChildren(CosmicQueryGraphOptions.Create().Result);
        if(accessBound)query=query.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result);
        if(query.IsError)throw new InvalidDataException(query.Message);
        var plan=CosmicQueryPlanner.Plan(query.Result,adapter.QueryProviderType,
            new[]{new CosmicQueryTarget(adapter.QueryProviderType,adapter.QueryCapabilities)});
        if(plan.IsError)throw new InvalidDataException(plan.Message);
        var read=await manager.ExecuteQueryAsync(plan.Result,new[]{adapter});
        if(read.IsError)throw new InvalidDataException(read.Message,read.Exception);
        var owner=read.Result.Providers.Single().Rows.Single();
        if(owner.Children.Count()!=1 || owner.Extras.Count!=(accessBound?0:1) ||
            HolonChangeTracking.HasChanges(owner))throw new InvalidDataException("Typed role shape or accepted baseline differs from selection.");
        if(owner.ChildRoleManifestJson==null || !owner.ChildRoleManifestJson.Contains("Extras"))
            throw new InvalidDataException("The full stored role observation was lost.");
        if(!accessBound)
        {
            if(owner.Extras[0].Label!="stored typed extra")throw new InvalidDataException("Registered typed metadata was not materialized.");
            owner.Extras[0].Label="edited locally";
            if(!HolonChangeTracking.HasChanges(owner))throw new InvalidDataException("Typed role edits were not tracked.");
        }
    }
}
