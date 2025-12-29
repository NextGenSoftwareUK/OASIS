namespace Application.Extensions.Mappers;

/// <summary>
/// Provides mapping functionality for converting data transfer objects (DTOs) into domain entities.
/// This class contains methods for mapping request models to entities such as User.
/// </summary>
public static class AccountMapper
{
    /// <summary>
    /// Maps a RegisterRequest DTO to a User entity.
    /// </summary>
    /// <param name="request">The RegisterRequest DTO containing registration data.</param>
    /// <param name="accessor">An IHttpContextAccessor to retrieve HTTP context-related information, such as the remote IP address.</param>
    /// <returns>A User entity mapped from the RegisterRequest DTO.</returns>
    public static User ToEntity(this RegisterRequest request, IHttpContextAccessor accessor)
    {
        return new()
        {
            CreatedBy = HttpAccessor.SystemId,
            Email = request.EmailAddress,
            UserName = request.UserName,
            PasswordHash = HashingUtility.ComputeSha256Hash(request.Password),
            TokenVersion = Guid.NewGuid(),
            CreatedByIp = accessor.GetRemoteIpAddress()
        };
    }
}