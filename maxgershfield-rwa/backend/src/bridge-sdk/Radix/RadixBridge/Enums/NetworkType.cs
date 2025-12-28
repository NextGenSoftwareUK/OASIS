namespace RadixBridge.Enums;

/// <summary>
/// Defines the types of networks supported in the Radix ecosystem.
/// Each network type is represented by a byte value.
/// </summary>
public enum NetworkType : byte
{
    /// <summary>
    /// Represents the main network (MainNet), used for production environments.
    /// </summary>
    Main = 0x01,

    /// <summary>
    /// Represents the test network (TestNet), used for development and testing.
    /// </summary>
    Test = 0x02
}