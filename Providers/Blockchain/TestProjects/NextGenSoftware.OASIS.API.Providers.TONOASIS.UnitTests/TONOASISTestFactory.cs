using System;
using System.IO;
namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests;

// Metadata-only fixture. These paths are never activated or treated as a native
// source. Real storage acceptance uses the explicit native runner/configuration.
internal static class TONOASISTestFactory
{
    public static TONOASIS Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "cosmic-ton-metadata-only");
        return new TONOASIS(new TonSandboxStorageOptions(Path.Combine(root, "node.exe"),
            Path.Combine(root, "native-worker.cjs"), Path.Combine(root, "binding.cjs"), root,
            Guid.NewGuid(), new string('0', 64)), () => new byte[32]);
    }
}
