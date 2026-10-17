using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

/// <summary>Shared lazy-controller acquisition inside trusted admission, before any BootLoader activation.</summary>
internal static class AdmittedStorageManagerAcquisition
{
    internal static bool TryCreate<T>(Func<OASISDNA> dna,Func<IOASISStorageProvider> provider,
        Func<IOASISStorageProvider,OASISDNA,T> factory,out T manager) where T:class
    {
        ArgumentNullException.ThrowIfNull(dna);ArgumentNullException.ThrowIfNull(provider);ArgumentNullException.ThrowIfNull(factory);
        manager=null;
        if(PreparedStorageExecution.Current==null)return false;
        var configuration=dna();
        if(configuration?.OASIS?.HyperDriveMode!=HyperDriveModes.V2)
            throw new UsageProtocolException(PreparedStorageExecution.RejectedCode,"Admitted storage manager acquisition requires HyperDrive V2.");
        manager=factory(provider(),configuration)
            ??throw new UsageProtocolException(PreparedStorageExecution.RejectedCode,"Admitted storage manager factory returned no manager.");
        return true;
    }
}
