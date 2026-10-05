namespace SolanaBridge;

/// <summary>
/// The ISolanaBridge interface defines methods for interacting with the Solana blockchain bridge.
/// It inherits common methods from the IBridge interface and can be extended to implement Solana-specific
/// operations such as deposits, withdrawals, and balance checks.
/// </summary>
public interface ISolanaBridge : IBridge
{
    // Additional Solana-specific methods can be added here if needed
}