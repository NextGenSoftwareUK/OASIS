using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

/// <summary>PostgreSQL-wire family: one Npgsql CRUD/query/serialization implementation.
/// Product-specific protocol acceptance must still run the shared suite on that product.</summary>
public abstract class PostgreSqlFamilyProviderBase : PostgreSQLOASIS
{
    protected PostgreSqlFamilyProviderBase(string connectionString,ProviderType providerType,string providerName)
        : base(connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ProviderType=new EnumValue<ProviderType>(providerType);
        ProviderName=providerName;
        ProviderDescription=providerName+" using the shared Npgsql PostgreSQL-wire storage implementation";
    }
}
