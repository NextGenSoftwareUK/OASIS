namespace Domain.Enums;

/// <summary>
/// Represents the different types of login providers available in the system.
/// This enum is used to identify the authentication source for the user login.
/// </summary>
public enum LoginProviderType
{
    /// <summary>
    /// The user logs in using the system's local authentication (username/password).
    /// </summary>
    Local,

    /// <summary>
    /// The user logs in using their Google account credentials.
    /// </summary>
    Google,

    /// <summary>
    /// The user logs in using their Facebook account credentials.
    /// </summary>
    Facebook,

    /// <summary>
    /// The user logs in using their GitHub account credentials.
    /// </summary>
    GitHub
}