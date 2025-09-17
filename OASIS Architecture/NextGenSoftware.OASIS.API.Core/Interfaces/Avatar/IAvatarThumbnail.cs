using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatarThumbnail
    {
        Guid Id { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string FullName { get; }
    }
}