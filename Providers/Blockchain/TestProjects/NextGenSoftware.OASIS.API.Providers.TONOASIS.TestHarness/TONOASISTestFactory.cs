using System;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.TestHarness;

internal static class TONOASISTestFactory
{
    private static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value : throw new InvalidOperationException("Explicit native configuration required: " + name);
    public static TONOASIS Create()
    {
        var options = new TonSandboxStorageOptions(Required("TONOASIS_NODE_EXECUTABLE"),
            Required("TONOASIS_WORKER_SCRIPT"), Required("TONOASIS_COMPILED_BINDING"), Required("TONOASIS_SOURCE_DIRECTORY"),
            Guid.Parse(Required("TONOASIS_SOURCE_ID")), Required("TONOASIS_CODE_HASH"), ReadOnly: true);
        return new TONOASIS(options, () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("TONOASIS_STORAGE_ENCRYPTION_KEY"));
    }
}
