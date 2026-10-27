using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

/// <summary>One provider-independent baseline. Native factories use the same calls as model factories.
/// No chain contracts, fee ledgers, routing or crash-recovery depth belongs in this suite.</summary>
public static class CoreStorageConformance
{
    public const string Suite = "core-storage-v1";
    public sealed class Report
    {
        public string SuiteVersion { get; set; } = Suite;
        public string Provider { get; set; } = "";
        public string Mode { get; set; } = "";
        public bool Synchronous { get; set; }
        public Guid OwnerId { get; set; } = Guid.NewGuid();
        public Guid RootId { get; set; } = Guid.NewGuid();
        public Guid ChildId { get; set; } = Guid.NewGuid();
        public Guid[] BatchIds { get; set; } = new[] { Guid.NewGuid(), Guid.NewGuid() };
        public Guid ImportId { get; set; } = Guid.NewGuid();
        public List<string> Passed { get; set; } = new();
        public string CurrentCase { get; set; } = "initial";
        public string FailureType { get; set; } = "";
        public bool Succeeded { get; set; }
    }

    public static async Task<Report> RunAsync(IOASISStorageProvider p, string provider, string mode, bool sync,
        Func<Report,Task> checkpoint)
    {
        ArgumentNullException.ThrowIfNull(p);ArgumentNullException.ThrowIfNull(checkpoint);
        if (mode is not ("controlled" or "native")) throw new ArgumentException("Execution mode must be explicit.",nameof(mode));
        var r=new Report { Provider=provider,Mode=mode,Synchronous=sync };
        var marker="conformance-"+r.OwnerId.ToString("N");
        var avatar=new Avatar { Id=r.OwnerId,Username=marker,Email=marker+"@example.invalid",FirstName=marker,IsActive=true };
        var detail=new AvatarDetail { Id=r.OwnerId,Username=avatar.Username,Email=avatar.Email,Karma=17,XP=31 };
        var root=new Holon { Id=r.RootId,Name=marker,CreatedByAvatarId=r.OwnerId,MetaData=new() { ["conformance"]=marker } };
        var child=new Holon { Id=r.ChildId,Name=marker+"-child",CreatedByAvatarId=r.OwnerId };
        root.Children.Add(child);
        async Task Step(string name,Func<Task> operation)
        {
            r.CurrentCase=name;await checkpoint(r); // Persist fixture identities/intent BEFORE dispatch.
            await operation();r.Passed.Add(name);await checkpoint(r);
        }
        try
        {
            await Step("activate",async()=> { var x=sync?p.ActivateProvider():await p.ActivateProviderAsync();Require(!x.IsError&&x.Result,"activation"); });
            await Step("avatar-save",async()=>Saved(sync?p.SaveAvatar(avatar):await p.SaveAvatarAsync(avatar),1));
            await Step("avatar-id",async()=>Require(Loaded(sync?p.LoadAvatar(r.OwnerId):await p.LoadAvatarAsync(r.OwnerId)).Id==r.OwnerId,"avatar ID"));
            await Step("avatar-username",async()=>Require(Loaded(sync?p.LoadAvatarByUsername(marker):await p.LoadAvatarByUsernameAsync(marker)).Id==r.OwnerId,"avatar username"));
            await Step("avatar-email",async()=>Require(Loaded(sync?p.LoadAvatarByEmail(avatar.Email):await p.LoadAvatarByEmailAsync(avatar.Email)).Id==r.OwnerId,"avatar email"));
            await Step("detail-save",async()=>Saved(sync?p.SaveAvatarDetail(detail):await p.SaveAvatarDetailAsync(detail),1));
            await Step("detail-id",async()=>Detail(Loaded(sync?p.LoadAvatarDetail(r.OwnerId):await p.LoadAvatarDetailAsync(r.OwnerId)),r.OwnerId));
            await Step("detail-username",async()=>Detail(Loaded(sync?p.LoadAvatarDetailByUsername(marker):await p.LoadAvatarDetailByUsernameAsync(marker)),r.OwnerId));
            await Step("detail-email",async()=>Detail(Loaded(sync?p.LoadAvatarDetailByEmail(avatar.Email):await p.LoadAvatarDetailByEmailAsync(avatar.Email)),r.OwnerId));
            await Step("graph-save",async()=>Saved(sync?p.SaveHolon(root,continueOnError:false):await p.SaveHolonAsync(root,continueOnError:false),2));
            await Step("holon-id",async()=>Require(Loaded(sync?p.LoadHolon(r.RootId,loadChildren:false):await p.LoadHolonAsync(r.RootId,loadChildren:false)).Name==marker,"holon payload"));
            root.Name=marker+"-updated";
            await Step("holon-update",async()=>Saved(sync?p.SaveHolon(root,saveChildren:false):await p.SaveHolonAsync(root,saveChildren:false),1));
            await Step("holon-update-read",async()=>Require(Loaded(sync?p.LoadHolon(r.RootId,loadChildren:false):await p.LoadHolonAsync(r.RootId,loadChildren:false)).Name==root.Name,"updated payload"));
            await Step("parent-query",async()=> {
                var x=sync?p.LoadHolonsForParent(r.RootId,loadChildren:false):await p.LoadHolonsForParentAsync(r.RootId,loadChildren:false);
                var rows=Collection(x);Require(rows.Count==1&&rows[0].Id==r.ChildId,"parent selection"); });
            await Step("metadata-query",async()=> {
                var x=sync?p.LoadHolonsByMetaData("conformance",marker,loadChildren:false):await p.LoadHolonsByMetaDataAsync("conformance",marker,loadChildren:false);
                var rows=Collection(x);Require(rows.Count==1&&rows[0].Id==r.RootId,"metadata selection"); });
            var batch=r.BatchIds.Select(id=>(IHolon)new Holon { Id=id,Name=marker+"-batch",CreatedByAvatarId=r.OwnerId,MetaData=null }).ToArray();
            await Step("batch-save-null-metadata",async()=>Saved(sync?p.SaveHolons(batch,saveChildren:false,continueOnError:false):await p.SaveHolonsAsync(batch,saveChildren:false,continueOnError:false),2));
            await Step("import",async()=> { var rows=new IHolon[] { new Holon { Id=r.ImportId,Name=marker+"-import",CreatedByAvatarId=r.OwnerId } };
                var x=sync?p.Import(rows):await p.ImportAsync(rows);Require(!x.IsError&&x.Result&&x.IsSaved,"import acknowledgement"); });
            await Step("owner-export",async()=> {
                var x=sync?p.ExportAllDataForAvatarById(r.OwnerId):await p.ExportAllDataForAvatarByIdAsync(r.OwnerId);
                var rows=Collection(x);var expected=new[]{r.RootId,r.ChildId,r.ImportId}.Concat(r.BatchIds).ToHashSet();
                Require(rows.Count==expected.Count&&rows.All(h=>h.CreatedByAvatarId==r.OwnerId)&&expected.SetEquals(rows.Select(h=>h.Id)),"owned export"); });
            // Healthy success path cleans only these exact owned identities. On failure preserve fixtures and stop.
            foreach(var id in new[]{r.ChildId,r.RootId,r.ImportId}.Concat(r.BatchIds))
                await Step("holon-delete-"+id.ToString("N"),async()=> { var x=sync?p.DeleteHolon(id):await p.DeleteHolonAsync(id);Require(!x.IsError&&x.IsDeleted,"delete acknowledgement");
                    var absent=sync?p.LoadHolon(id,loadChildren:false):await p.LoadHolonAsync(id,loadChildren:false);Require(absent.Result==null&&!absent.IsLoaded,"deleted holon absence"); });
            await Step("avatar-delete",async()=> { var x=sync?p.DeleteAvatar(r.OwnerId,softDelete:false):await p.DeleteAvatarAsync(r.OwnerId,softDelete:false);Require(!x.IsError&&x.Result&&x.IsDeleted,"avatar delete"); });
            await Step("avatar-detail-absence",async()=> {var a=sync?p.LoadAvatar(r.OwnerId):await p.LoadAvatarAsync(r.OwnerId);var d=sync?p.LoadAvatarDetail(r.OwnerId):await p.LoadAvatarDetailAsync(r.OwnerId);Require(a.Result==null&&!a.IsLoaded&&d.Result==null&&!d.IsLoaded,"avatar/detail cleanup"); });
            await Step("deactivate",async()=> {var x=sync?p.DeActivateProvider():await p.DeActivateProviderAsync();Require(!x.IsError&&x.Result,"deactivation"); });
            r.CurrentCase="complete";r.Succeeded=true;await checkpoint(r);
        }
        catch(Exception error)
        {
            // No fabricated success, retries, raw-prefix sweeps or error-message/credential dumping.
            r.FailureType=error.GetType().FullName??error.GetType().Name;await checkpoint(r);
        }
        return r;
    }
    private static void Require(bool value,string contract) {if(!value)throw new InvalidOperationException("Storage conformance failed: "+contract);}
    private static void Saved<T>(OASISResult<T> x,int count)=>Require(x!=null&&!x.IsError&&x.IsSaved&&x.SavedCount==count,"save acknowledgement/count");
    private static T Loaded<T>(OASISResult<T> x) where T:class {Require(x!=null&&!x.IsError&&x.IsLoaded&&x.LoadedCount==1&&x.Result!=null,"single read acknowledgement/count");return x.Result;}
    private static List<IHolon> Collection(OASISResult<IEnumerable<IHolon>> x) {Require(x!=null&&!x.IsError&&x.IsLoaded&&x.Result!=null,"collection acknowledgement");var rows=x.Result.ToList();Require(x.LoadedCount==rows.Count,"collection count");return rows;}
    private static void Detail(IAvatarDetail d,Guid id)=>Require(d.Id==id&&d.Karma==17&&d.XP==31,"avatar detail payload");
}
