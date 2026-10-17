using System;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Unity
{
    public sealed class UnityEdgeLocalProviderOptions
    {
        public string ProviderId { get; set; }
        public byte[] AppBundle { get; set; }
        public string InstalledAppId { get; set; } = "oasis";
        public string NetworkSeed { get; set; }
        public string BootstrapUrl { get; set; }
        public string RelayUrl { get; set; }
    }

    /// <summary>
    /// AOT-safe registration boundary between an application and deployment-profile-specific
    /// local providers. Profiles register real implementations during Unity startup.
    /// </summary>
    public static class UnityEdgeLocalProviderFactory
    {
        private static readonly object Gate = new object();
        private static string _providerId;
        private static Func<UnityEdgeLocalProviderOptions, OASISResult<IUnityEdgeLocalProviderLifecycle>> _factory;

        public static void Register(string providerId,
            Func<UnityEdgeLocalProviderOptions, OASISResult<IUnityEdgeLocalProviderLifecycle>> factory)
        {
            if (string.IsNullOrWhiteSpace(providerId)) throw new ArgumentException("A provider id is required.", nameof(providerId));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            lock (Gate)
            {
                if (_factory != null && !string.Equals(_providerId, providerId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Local provider '{_providerId}' is already registered.");
                _providerId = providerId;
                _factory = factory;
            }
        }

        public static OASISResult<IUnityEdgeLocalProviderLifecycle> Create(UnityEdgeLocalProviderOptions options)
        {
            if (options == null || string.IsNullOrWhiteSpace(options.ProviderId))
                return Error("UNITY_EDGE_LOCAL_PROVIDER_REQUIRED", "A local provider id is required.");
            lock (Gate)
            {
                if (_factory == null || !string.Equals(_providerId, options.ProviderId, StringComparison.OrdinalIgnoreCase))
                    return Error("UNITY_EDGE_LOCAL_PROVIDER_NOT_PACKAGED",
                        $"Local provider '{options.ProviderId}' is not present in this deployment profile.");
                return _factory(options);
            }
        }

        private static OASISResult<IUnityEdgeLocalProviderLifecycle> Error(string code, string message) =>
            new OASISResult<IUnityEdgeLocalProviderLifecycle>
            { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    }
}
