using NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS;

/// <summary>Thin PostgreSQL-wire adapter; native product conformance is tracked separately.</summary>
public class CockroachDBOASIS : PostgreSqlFamilyProviderBase
{
    public CockroachDBOASIS(string connectionString)
        : base(connectionString,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.CockroachDBOASIS,nameof(CockroachDBOASIS)) { }
}
