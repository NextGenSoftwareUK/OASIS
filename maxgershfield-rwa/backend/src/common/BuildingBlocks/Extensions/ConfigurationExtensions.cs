using BuildingBlocks.Extensions.Resources;

namespace BuildingBlocks.Extensions;

public static class ConfigurationExtensions
{
    private const string DefaultConnectionName = "DefaultConnection";

    /// <summary>
    /// Retrieves the required string value for the specified configuration key.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="key">The key of the configuration value.</param>
    /// <returns>The non-empty string value associated with the specified key.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the configuration value is null, empty, or consists only of white-space characters.
    /// </exception>
    public static string GetRequiredString(this IConfiguration configuration, string key)
    {
        string? value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(Messages.ConfigurationValueRequired(key));

        return value;
    }

    /// <summary>
    /// Retrieves the required integer value for the specified configuration key.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="key">The key of the configuration value.</param>
    /// <returns>The parsed integer value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the value is missing or cannot be parsed as a valid integer.
    /// </exception>
    public static int GetRequiredInt(this IConfiguration configuration, string key)
    {
        string? value = configuration[key];
        if (!int.TryParse(value, out int result))
            throw new InvalidOperationException(Messages.ConfigurationValueMustBeInteger(key, value));

        return result;
    }

    /// <summary>
    /// Retrieves the required boolean value for the specified configuration key.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="key">The key of the configuration value.</param>
    /// <returns>The parsed boolean value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the value is missing or cannot be parsed as a valid boolean.
    /// </exception>
    public static bool GetRequiredBool(this IConfiguration configuration, string key)
    {
        string? value = configuration[key];
        if (!bool.TryParse(value, out bool result))
            throw new InvalidOperationException(Messages.ConfigurationValueMustBeBoolean(key, value));

        return result;
    }

    /// <summary>
    /// Retrieves the specified connection string from the configuration, or throws an exception if it is not found.
    /// </summary>
    /// <param name="configuration">The application configuration instance.</param>
    /// <param name="name">The name of the connection string to retrieve.</param>
    /// <returns>The connection string associated with the specified name.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection string with the specified name is not found in the configuration.
    /// </exception>
    public static string GetRequiredConnectionString(this IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException(Messages.ConnectionStringNotFound(name));

    /// <summary>
    /// Retrieves the default connection string from the configuration, or throws an exception if it is not found.
    /// </summary>
    /// <param name="configuration">The application configuration instance.</param>
    /// <returns>The default connection string.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the default connection string is not found in the configuration.
    /// </exception>
    public static string GetDefaultConnectionString(this IConfiguration configuration) =>
        configuration.GetRequiredConnectionString(DefaultConnectionName);
}