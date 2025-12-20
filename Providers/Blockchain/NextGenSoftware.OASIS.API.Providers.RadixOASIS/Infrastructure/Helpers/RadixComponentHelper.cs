using System;
using System.Collections.Generic;
using RadixEngineToolkit;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;

/// <summary>
/// Helper class for Radix component interactions
/// </summary>
public static class RadixComponentHelper
{
    /// <summary>
    /// Converts C# objects to Radix Scrypto argument format for CallMethod
    /// Handles basic types: ulong, string, int, bool, etc.
    /// </summary>
    public static object[] ConvertToScryptoArgs(List<object> args)
    {
        if (args == null || args.Count == 0)
            return Array.Empty<object>();

        var scryptoArgs = new object[args.Count];
        
        for (int i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            
            // Convert types to Scrypto-compatible formats
            switch (arg)
            {
                case ulong ulongValue:
                    scryptoArgs[i] = ulongValue;
                    break;
                case long longValue:
                    scryptoArgs[i] = (ulong)longValue;
                    break;
                case int intValue:
                    scryptoArgs[i] = (ulong)intValue;
                    break;
                case uint uintValue:
                    scryptoArgs[i] = (ulong)uintValue;
                    break;
                case string stringValue:
                    scryptoArgs[i] = stringValue;
                    break;
                case bool boolValue:
                    scryptoArgs[i] = boolValue;
                    break;
                default:
                    // For other types, convert to string (JSON strings for complex objects)
                    scryptoArgs[i] = arg.ToString();
                    break;
            }
        }
        
        return scryptoArgs;
    }
}

