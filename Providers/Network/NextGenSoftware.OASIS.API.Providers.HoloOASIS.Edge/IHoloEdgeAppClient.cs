using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge
{
    /// <summary>
    /// Small, testable app-interface boundary. Implementations use an already authenticated
    /// Holochain app websocket; conductor ownership remains with the platform lifecycle host.
    /// </summary>
    public interface IHoloEdgeAppClient : System.IAsyncDisposable
    {
        Task<OASISResult<bool>> CallAsync(string zome, string function,
            IReadOnlyDictionary<string, object> payload, CancellationToken cancellationToken);
    }
}
