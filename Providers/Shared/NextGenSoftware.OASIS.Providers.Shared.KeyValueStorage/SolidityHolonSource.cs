using System;
using System.Text;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Shared Solidity source emission, not compilation, deployment or access-control approval.</summary>
public static class SolidityHolonSource
{
    public static void AppendInitialization(StringBuilder source,string storageExpression)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageExpression);
        // Clear the entire prior value so additional schema fields receive Solidity defaults.
        // A positional three-argument constructor is invalid when the schema adds fields.
        source.AppendLine($"        delete {storageExpression};");
        source.AppendLine($"        {storageExpression}.id = id;");
        source.AppendLine($"        {storageExpression}.name = name;");
        source.AppendLine($"        {storageExpression}.description = description;");
    }
}
