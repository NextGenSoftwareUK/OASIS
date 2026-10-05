namespace Application.Extensions.Mappers;

/// <summary>
/// Provides mapping functionality for converting user-related request models into domain entities.
/// This class contains methods for mapping User entities to response models and updating user profile data.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Maps a User entity to a GetAllUserResponse.
    /// </summary>
    /// <param name="user">The User entity to be mapped.</param>
    /// <returns>A GetAllUserResponse containing the mapped data.</returns>
    public static GetAllUserResponse ToRead(this User user)
    {
        return new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.UserName,
            user.Dob);
    }

    /// <summary>
    /// Maps a User entity to a GetUserDetailPrivateResponse, which includes sensitive data like the last login and total logins.
    /// </summary>
    /// <param name="user">The User entity to be mapped.</param>
    /// <returns>A GetUserDetailPrivateResponse containing the mapped data.</returns>
    public static GetUserDetailPrivateResponse ToReadPrivateDetail(this User user)
    {
        return new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.UserName,
            user.Dob,
            user.LastLoginAt,
            user.TotalLogins);
    }

    /// <summary>
    /// Maps a User entity to a GetUserDetailPublicResponse, which contains public details such as name and contact information.
    /// </summary>
    /// <param name="user">The User entity to be mapped.</param>
    /// <returns>A GetUserDetailPublicResponse containing the mapped data.</returns>
    public static GetUserDetailPublicResponse ToReadPublicDetail(this User user)
    {
        return new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.UserName,
            user.Dob);
    }

    /// <summary>
    /// Maps an UpdateUserProfileRequest to an existing User entity.
    /// </summary>
    /// <param name="user">The User entity to be updated.</param>
    /// <param name="request">The UpdateUserProfileRequest containing the new data.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <returns>The updated User entity.</returns>
    public static User ToEntity(this User user, UpdateUserProfileRequest request, IHttpContextAccessor accessor)
    {
        user.Update(accessor.GetId());
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.Dob = request.Dob;
        user.UpdatedByIp!.Add(accessor.GetRemoteIpAddress());
        return user;
    }
}
