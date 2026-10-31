using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

/// <summary>Lazy V2 controller acquisition before BootLoader activation.
/// Admission-only callers retain their strict context guard. Unbound routing
/// controllers may acquire V2 managers for ordinary reads as well.</summary>
internal static class AdmittedStorageManagerAcquisition
{
    internal static bool TryCreate<T>(Func<OASISDNA> dna,Func<IOASISStorageProvider> provider,
        Func<IOASISStorageProvider,OASISDNA,T> factory,out T manager, bool admissionOnly = true) where T:class
    {
        ArgumentNullException.ThrowIfNull(dna);ArgumentNullException.ThrowIfNull(provider);ArgumentNullException.ThrowIfNull(factory);
        manager=null;
        var admitted = PreparedStorageExecution.Current != null;
        if(!admitted && admissionOnly)return false;
        var configuration=dna();
        if(configuration?.OASIS?.HyperDriveMode!=HyperDriveModes.V2)
        {
            if(!admitted)return false;
            throw new UsageProtocolException(PreparedStorageExecution.RejectedCode,"Admitted storage manager acquisition requires HyperDrive V2.");
        }
        manager=factory(provider(),configuration)
            ??throw new UsageProtocolException(PreparedStorageExecution.RejectedCode,"Admitted storage manager factory returned no manager.");
        return true;
    }
}
