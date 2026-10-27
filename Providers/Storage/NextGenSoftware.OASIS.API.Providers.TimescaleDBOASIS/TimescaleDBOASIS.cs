using NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TimescaleDBOASIS;

/// <summary>Thin PostgreSQL-wire adapter; native product conformance is tracked separately.</summary>
public class TimescaleDBOASIS : PostgreSqlFamilyProviderBase
{
    public TimescaleDBOASIS(string connectionString)
        : base(connectionString,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TimescaleDBOASIS,nameof(TimescaleDBOASIS)) { }
}
