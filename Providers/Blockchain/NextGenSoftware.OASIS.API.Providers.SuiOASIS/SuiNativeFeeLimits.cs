namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

/// <summary>Trusted provider configuration. Per-built-transaction MIST budget ceiling, not an aggregate
/// operation reservation or permission to sponsor fees. Zero is invalid; no limits are inferred.</summary>
public sealed record SuiNativeFeeLimits(ulong MaximumGasBudgetMist);
