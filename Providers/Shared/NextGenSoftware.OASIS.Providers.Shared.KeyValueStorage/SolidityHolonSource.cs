using System;
using System.Text;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.Utilities.ExtentionMethods;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Shared Solidity source emission, not compilation, deployment or access-control approval.</summary>
public static class SolidityHolonSource
{
    /// <summary>Checks collisions in the exact emitted namespaces; does not certify compilation or access control.</summary>
    public static bool HasUniqueSchemaNames(ICelestialBody body,string fallbackContractName)
    {
        ArgumentNullException.ThrowIfNull(body);
        var symbols=new HashSet<string>(StringComparer.Ordinal){body.Name?.ToPascalCase()??fallbackContractName};
        var zomes=body.CelestialBodyCore?.Zomes;
        if(zomes==null)return true;
        foreach(var zome in zomes){
            if(zome?.Children==null)continue;
            foreach(var holon in zome.Children){
                if(holon==null||string.IsNullOrWhiteSpace(holon.Name))continue;
                var type=holon.Name.ToPascalCase();var variable=type.ToCamelCase();
                foreach(var symbol in new[]{type,variable+"s",variable+"Ids","create"+type,"get"+type,"update"+type,"delete"+type})
                    if(!symbols.Add(symbol))return false;
                var members=new HashSet<string>(StringComparer.Ordinal){"id","name","description"};
                if(holon.Nodes!=null)foreach(var node in holon.Nodes)
                    if(node!=null&&!string.IsNullOrWhiteSpace(node.NodeName)&&!members.Add(node.NodeName.ToSnakeCase()))return false;
            }
        }
        return true;
    }

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
