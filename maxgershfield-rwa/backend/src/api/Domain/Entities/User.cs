namespace Domain.Entities;

/// <summary>
/// Represents a user within the system.
/// This entity stores detailed information about the user, including personal details, security settings, and activity data.
/// </summary>
public sealed class User : BaseEntity
{
    /// <summary>
    /// The user's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// The user's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// The user's email address.
    /// This is used for communication and as a primary identifier.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The user's phone number.
    /// This can be used for communication or as a secondary identifier.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The user's username.
    /// This is used for login and as a unique identifier in the system.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// The user's date of birth.
    /// This field is optional.
    /// </summary>
    public DateTimeOffset? Dob { get; set; }

    /// <summary>
    /// Indicates whether the user's email address has been confirmed.
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Indicates whether the user's phone number has been confirmed.
    /// </summary>
    public bool PhoneNumberConfirmed { get; set; }

    /// <summary>
    /// The hash of the user's password.
    /// This is used to securely store and validate the user's password.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp of the last password change.
    /// </summary>
    public DateTimeOffset? LastPasswordChangeAt { get; set; }

    /// <summary>
    /// The timestamp of the last login.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Indicates whether the user's account is locked.
    /// </summary>
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// The timestamp when the lockout ends, if applicable.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// The total number of logins the user has made.
    /// </summary>
    public long TotalLogins { get; set; }

    /// <summary>
    /// The secret key for two-factor authentication (if enabled).
    /// </summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// Indicates whether two-factor authentication is enabled for the user.
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// The version of the user's token.
    /// This is used to track changes to the user's authentication token.
    /// </summary>
    public Guid TokenVersion { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The collection of roles assigned to the user.
    /// Each role defines a set of permissions and responsibilities for the user.
    /// </summary>
    public HashSet<UserRole> UserRoles { get; } = new HashSet<UserRole>();

    /// <summary>
    /// The collection of claims associated with the user.
    /// Claims represent specific permissions or attributes granted to the user.
    /// </summary>
    public ICollection<UserClaim> UserClaims { get; } = new List<UserClaim>();

    /// <summary>
    /// The collection of login entries for the user.
    /// This stores information about how the user has authenticated (e.g., external login providers).
    /// </summary>
    public ICollection<UserLogin> UserLogins { get; } = new List<UserLogin>();

    /// <summary>
    /// The collection of tokens associated with the user.
    /// This can include authentication tokens or other types of user-specific tokens.
    /// </summary>
    public ICollection<UserToken> UserTokens { get; } = new List<UserToken>();

    /// <summary>
    /// The collection of verification codes associated with the user.
    /// These codes are typically used for account recovery or multi-factor authentication.
    /// </summary>
    public ICollection<UserVerificationCode> UserVerificationCodes { get; } = new List<UserVerificationCode>();

    /// <summary>
    /// The collection of virtual accounts associated with the user.
    /// These accounts represent the user's assets or balances across different networks.
    /// </summary>
    public ICollection<VirtualAccount> VirtualAccounts { get; } = new List<VirtualAccount>();

    /// <summary>
    /// The collection of orders made by the user.
    /// Orders represent transactions or requests made by the user, typically involving exchanges or purchases.
    /// </summary>
    public ICollection<Order> Orders { get; } = new List<Order>();

    /// <summary>
    /// The collection of wallet-linked accounts associated with the user.
    /// These accounts represent the user's linked wallets, such as cryptocurrency wallets.
    /// </summary>
    public ICollection<WalletLinkedAccount> WalletLinkedAccounts { get; } = new List<WalletLinkedAccount>();
}
