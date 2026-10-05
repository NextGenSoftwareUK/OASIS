namespace Application.DTOs.User.Requests;

/// <summary>
/// Represents the data required to update a user's profile information.
/// </summary>
/// <param name="FirstName">
/// New first name of the user. Cannot be null or empty.
/// </param>
/// <param name="LastName">
/// New last name of the user. Cannot be null or empty.
/// </param>
/// <param name="Email">
/// New email address. Must be unique and in a valid format.
/// </param>
/// <param name="PhoneNumber">
/// New phone number. Must be unique and properly formatted.
/// </param>
/// <param name="UserName">
/// New username. Must be unique within the system.
/// </param>
/// <param name="Dob">
/// Optional updated date of birth of the user.
/// </param>
public record UpdateUserProfileRequest(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string UserName,
    DateTimeOffset? Dob);