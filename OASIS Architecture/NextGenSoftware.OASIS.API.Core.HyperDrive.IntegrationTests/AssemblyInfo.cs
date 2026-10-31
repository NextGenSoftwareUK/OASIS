using Xunit;

// These live tests share retained node state, fixture leases and SDK/global-manager settings.
// Serialize within the host instead of allocating duplicate environments for concurrent tests.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
