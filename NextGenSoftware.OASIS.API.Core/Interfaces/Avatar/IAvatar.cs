using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatar
    {
        bool AcceptTerms { get; set; }
        string Email { get; set; }
        string FirstName { get; set; }
        string FullName { get; }
        Guid Id { get; set; }
        bool IsVerified { get; }
        string JwtToken { get; set; }
        string LastName { get; set; }
        string Name { get; }
        string Password { get; set; }
        DateTime? PasswordReset { get; set; }
        string RefreshToken { get; set; }
        List<RefreshToken> RefreshTokens { get; set; }
        string ResetToken { get; set; }
        DateTime? ResetTokenExpires { get; set; }
        string Title { get; set; }
        string Username { get; set; }
        string VerificationToken { get; set; }
        DateTime? Verified { get; set; }

        bool OwnsToken(string token);
        IAvatar Save();
        Task<IAvatar> SaveAsync();
    }
}