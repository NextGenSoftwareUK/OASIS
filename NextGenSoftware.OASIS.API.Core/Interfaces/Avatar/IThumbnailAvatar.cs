namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IThumbnailAvatar
    {
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string FullName { get; }
    }
}