using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    /// <summary>
    /// Interface for HyperDrive routed requests
    /// </summary>
    public interface IRequest 
    {
        string RequestType { get; set; }
        int Priority { get; set; }
        string ProviderType { get; set; }
        Dictionary<string, object> Parameters { get; set; }
    }
}



