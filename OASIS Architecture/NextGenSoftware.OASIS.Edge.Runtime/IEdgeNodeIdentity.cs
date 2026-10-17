using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    /// <summary>
    /// Platform-secure ONET identity. Unity hosts implement this with Keychain/Keystore-backed signing;
    /// the Edge Runtime never persists private-key material in SQLite or ordinary files.
    /// </summary>
    public interface IEdgeNodeIdentity
    {
        string NodeId { get; }
        string PublicKey { get; }
        Task<OASISResult<string>> SignAsync(string message, CancellationToken cancellationToken);
    }
}
