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

    public static void ConfigureOrdinary(Holon source)
    {
        Configure(source);
        // Force a collection order opposite to database identity sorting.
        source.Children[0].Id=Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffffe");
        source.Children[1].Id=Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    public static async Task VerifyOrdinaryAsync(HolonManager manager,ICosmicQueryStorageProvider adapter,
        CqlRoleOwner source,Func<Task<IHolon>> save)
    {
        var children=source.Children.Select(child=>child.Id).ToArray();
        var extras=source.Extras.Select(child=>child.Id).ToArray();
        var expected=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,source.Id,new[] {
            new HolonChildRole("Children",children),new HolonChildRole("Extras",extras)}));
        source.Description="ordinary typed ownership write";
        var acknowledged=await save();
        if(source.ChildRoleManifestJson!=expected || (acknowledged as Holon)?.ChildRoleManifestJson!=expected)
            throw new InvalidDataException("Ordinary acknowledgement lost logical custom roles or collection order.");
        HolonChangeTracking.ValidateChildSelectionForSave(source);
        var id=source.Id;
        var query=CosmicQuery<CqlRoleOwner>.Create().Where(row=>row.Id==id)
            .WithChildren(CosmicQueryGraphOptions.Create().Result);
        if(query.IsError)throw new InvalidDataException(query.Message);
        var plan=CosmicQueryPlanner.Plan(query.Result,adapter.QueryProviderType,
            new[]{new CosmicQueryTarget(adapter.QueryProviderType,adapter.QueryCapabilities)});
        if(plan.IsError)throw new InvalidDataException(plan.Message);
        var read=await manager.ExecuteQueryAsync(plan.Result,new[]{adapter});
        if(read.IsError)throw new InvalidDataException(read.Message,read.Exception);
        var owner=read.Result.Providers.Single().Rows.Single();
        if(owner.Description!=source.Description || owner.ChildRoleManifestJson!=expected
            || !owner.Children.Select(child=>child.Id).SequenceEqual(children)
            || !owner.Extras.Select(child=>child.Id).SequenceEqual(extras)
            || owner.Extras.Single().Label!="stored typed extra" || HolonChangeTracking.HasChanges(owner))
            throw new InvalidDataException("Native typed query did not preserve committed custom roles, order, values and clean baseline.");
        owner.Extras.Single().Label="changed after query";
        if(!HolonChangeTracking.HasChanges(owner))
            throw new InvalidDataException("The typed custom child edit was not tracked through its owner.");
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
