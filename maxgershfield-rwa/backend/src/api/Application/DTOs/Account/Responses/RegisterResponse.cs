namespace Application.DTOs.Account.Responses
{
    /// <summary>
    /// Represents the response returned after a successful user registration.
    /// Contains the unique identifier of the newly registered user.
    /// </summary>
    /// <param name="UserId">
    /// The unique identifier assigned to the registered user.
    /// </param>
    public sealed record RegisterResponse(Guid UserId);
}