using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;

public partial class LocalFileOASIS
{
    private async Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>> SaveSelectedGraphAsync(
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolon source,bool children,bool recursive,int depth)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>();
        var acknowledged=new List<string>();string? dispatched=null;
        try
        {
            var plan=HolonGraphWritePlan.Create(new[] {source},children,recursive,depth,0);
            var staged=new List<(PlannedHolonWrite Node,string Path,string Json)>();
            foreach(var node in plan.Nodes)
            {
                var path=Path.Combine(_holonDirectory,node.Snapshot.Id+".json");
                if(File.Exists(path))
                {
                    var original=OasisJson.DeserializeStored<NextGenSoftware.OASIS.API.Core.Holons.Holon>(await File.ReadAllTextAsync(path))
                        ?? throw new System.IO.InvalidDataException("Stored holon is null.");
                    node.Snapshot.CreatedDate=original.CreatedDate;node.Snapshot.CreatedByAvatarId=original.CreatedByAvatarId;
                    node.Snapshot.CreatedProviderType=original.CreatedProviderType;
                }
                else
                {
                    if(node.Snapshot.CreatedDate==DateTime.MinValue) node.Snapshot.CreatedDate=DateTime.UtcNow;
                    node.Snapshot.ModifiedDate=DateTime.MinValue;node.Snapshot.ModifiedByAvatarId=Guid.Empty;
                }
                node.Snapshot.ProviderUniqueStorageKey ??= new();
                node.Snapshot.ProviderUniqueStorageKey.TryAdd(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.LocalFileOASIS,node.Snapshot.Id.ToString());
                staged.Add((node,path,OasisJson.Serialize(node.Snapshot)));
            }
            if(!IsProviderActivated)
            {
                var active=await ActivateProviderAsync();
                if(active.IsError) {NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,active.Message,active.Exception);return result;}
            }
            Directory.CreateDirectory(_holonDirectory);
            foreach(var write in staged)
            {
                dispatched=write.Path;
                await File.WriteAllTextAsync(write.Path,write.Json);
                acknowledged.Add(write.Path);dispatched=null;
                write.Node.Source.Id=write.Node.Snapshot.Id;write.Node.Source.ParentHolonId=write.Node.Snapshot.ParentHolonId;
                write.Node.Source.CreatedDate=write.Node.Snapshot.CreatedDate;write.Node.Source.CreatedByAvatarId=write.Node.Snapshot.CreatedByAvatarId;
                write.Node.Source.CreatedProviderType=write.Node.Snapshot.CreatedProviderType;
                write.Node.Source.ProviderUniqueStorageKey ??= new();
                write.Node.Source.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.LocalFileOASIS]=write.Node.Snapshot.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.LocalFileOASIS];
            }
            result.Result=source;result.IsSaved=true;result.SavedCount=acknowledged.Count;
        }
        catch(Exception cause)
        {
            Exception failure=acknowledged.Count!=0 || dispatched!=null ? new LocalGraphWriteReconciliationException(acknowledged,dispatched,cause):cause;
            NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,cause.Message,failure);
            if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}

public sealed class LocalGraphWriteReconciliationException:Exception,NextGenSoftware.OASIS.API.Core.Interfaces.INativeMutationReconciliationRequired
{
    public IReadOnlyList<string> AcknowledgedPaths {get;}
    public string? UncertainPath {get;}
    public string OriginalTransactionId=>string.Empty;
    public LocalGraphWriteReconciliationException(IEnumerable<string> paths,string? uncertain,Exception cause)
        :base("Local graph write requires native reconciliation before replay.",cause)
    {AcknowledgedPaths=Array.AsReadOnly(paths.ToArray());UncertainPath=uncertain;}
}
