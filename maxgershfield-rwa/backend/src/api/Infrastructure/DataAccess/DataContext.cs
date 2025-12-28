using Role = Domain.Entities.Role;
using Trust = Domain.Entities.Trust;
using TrustProperty = Domain.Entities.TrustProperty;
using ContractGeneration = Domain.Entities.ContractGeneration;
using CorporateAction = Domain.Entities.CorporateAction;
using EquityPrice = Domain.Entities.EquityPrice;
using FundingRate = Domain.Entities.FundingRate;
using RiskWindow = Domain.Entities.RiskWindow;
using RiskFactor = Domain.Entities.RiskFactor;
using RiskRecommendation = Domain.Entities.RiskRecommendation;

namespace Infrastructure.DataAccess;

/// <summary>
/// Main Entity Framework Core context for the application.
///
/// Provides DbSet properties for accessing all key entities (Users, Roles, Orders, etc.)
/// and handles database operations like reading, writing, and updating data.
///
/// Applies entity configurations from the Infrastructure assembly and sets up
/// a global filter for soft-deleted records.
///
/// Acts as a Unit of Work in data transactions.
/// </summary>
public sealed class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    /// <summary>
    /// Users registered in the system.
    /// Each user can have multiple roles, tokens, logins, claims, and verification codes.
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Relationship between users and roles.
    /// Implements many-to-many user-role assignments.
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; }

    /// <summary>
    /// Access and refresh tokens issued to users for authentication.
    /// </summary>
    public DbSet<UserToken> UserTokens { get; set; }

    /// <summary>
    /// Claims attached to individual users (e.g., for custom authorization scenarios).
    /// </summary>
    public DbSet<UserClaim> UserClaims { get; set; }

    /// <summary>
    /// External login providers associated with users (e.g., Google, Facebook).
    /// </summary>
    public DbSet<UserLogin> UserLogins { get; set; }

    /// <summary>
    /// Verification codes sent to users for email/phone confirmation or password reset.
    /// </summary>
    public DbSet<UserVerificationCode> UserVerificationCodes { get; set; }

    /// <summary>
    /// Roles that define access levels and permissions within the system.
    /// </summary>
    public DbSet<Role> Roles { get; set; }

    /// <summary>
    /// Claims attached to roles (used for fine-grained permission management).
    /// </summary>
    public DbSet<RoleClaim> RoleClaims { get; set; }

    /// <summary>
    /// Supported blockchain or payment networks (e.g., Ethereum, Visa, SWIFT).
    /// </summary>
    public DbSet<Network> Networks { get; set; }

    /// <summary>
    /// API tokens or credentials related to networks.
    /// </summary>
    public DbSet<NetworkToken> NetworkTokens { get; set; }

    /// <summary>
    /// Balances associated with user accounts in specific networks.
    /// </summary>
    public DbSet<AccountBalance> AccountBalances { get; set; }

    /// <summary>
    /// Internal virtual accounts representing wallets or deposit channels for users.
    /// </summary>
    public DbSet<VirtualAccount> VirtualAccounts { get; set; }

    /// <summary>
    /// Orders placed by users, potentially involving asset exchange or delivery.
    /// </summary>
    public DbSet<Order> Orders { get; set; }

    /// <summary>
    /// Exchange rates between different assets, currencies, or tokens.
    /// </summary>
    public DbSet<ExchangeRate> ExchangeRates { get; set; }

    /// <summary>
    /// External financial accounts linked to the user's wallet (e.g., bank account, card, external wallet).
    /// </summary>
    public DbSet<WalletLinkedAccount> WalletLinkedAccounts { get; set; }

    /// <summary>
    /// Real World Assets Tokens 
    /// </summary>
    public DbSet<RwaToken> RwaTokens { get; set; }

    public DbSet<RwaTokenPriceHistory> RwaTokenPriceHistories { get; set; }

    public DbSet<RwaTokenOwnershipTransfer> RwaTokenOwnershipTransfers { get; set; }

    /// <summary>
    /// Fractional ownership records for fractionalized NFTs
    /// </summary>
    public DbSet<FractionalOwnership> FractionalOwnerships { get; set; }

    /// <summary>
    /// Trust entities for smart trust management
    /// </summary>
    public DbSet<Trust> Trusts { get; set; }

    /// <summary>
    /// Trust properties associated with trusts
    /// </summary>
    public DbSet<TrustProperty> TrustProperties { get; set; }

    /// <summary>
    /// Contract generation records for trust contracts
    /// </summary>
    public DbSet<ContractGeneration> ContractGenerations { get; set; }

    /// <summary>
    /// Corporate actions (splits, dividends, mergers) that affect equity prices
    /// </summary>
    public DbSet<CorporateAction> CorporateActions { get; set; }

    /// <summary>
    /// Equity prices with raw and adjusted values
    /// </summary>
    public DbSet<EquityPrice> EquityPrices { get; set; }

    /// <summary>
    /// Funding rates for perpetual futures on RWAs/equities
    /// </summary>
    public DbSet<FundingRate> FundingRates { get; set; }

    /// <summary>
    /// Risk windows for identifying periods of elevated risk
    /// </summary>
    public DbSet<RiskWindow> RiskWindows { get; set; }

    /// <summary>
    /// Risk factors that contribute to risk windows
    /// </summary>
    public DbSet<RiskFactor> RiskFactors { get; set; }

    /// <summary>
    /// Risk recommendations for deleveraging and returning to baseline
    /// </summary>
    public DbSet<RiskRecommendation> RiskRecommendations { get; set; }

    /// <summary>
    /// Configures the EF Core model by applying all entity configurations and global filters.
    ///
    /// This method is invoked by the Entity Framework runtime when the model for a derived context is being created.
    /// It performs two primary tasks:
    /// 
    /// 1. <b>ApplyConfigurationsFromAssembly</b>:
    ///     - Scans the <c>Infrastructure</c> assembly for all classes implementing <c>IEntityTypeConfiguration&lt;TEntity&gt;</c>.
    ///     - Automatically applies fluent configurations for entities without needing to register them one by one.
    ///     - Ensures centralized and maintainable model configuration.
    ///
    /// 2. <b>FilterSoftDeletedProperties</b>:
    ///     - Applies a global query filter to exclude entities marked as "soft-deleted".
    ///     - Ensures soft-deleted data does not appear in query results unless explicitly requested.
    ///
    /// <para>
    /// This setup promotes separation of concerns, modular configuration, and consistent handling of deleted records.
    /// </para>
    ///
    /// <remarks>
    /// Any new configuration classes should be placed in the <c>Infrastructure</c> project and implement
    /// <c>IEntityTypeConfiguration&lt;T&gt;</c>. They will be picked up automatically during application startup.
    /// </remarks>
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Infrastructure).Assembly);
        modelBuilder.FilterSoftDeletedProperties();
    }
}