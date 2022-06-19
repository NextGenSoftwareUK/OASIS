using System;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities
{
    [Table("avatars")]
    public class AvatarEntity
    {
        [Column("id", TypeName = "NVARCHAR(64)")]
        public string Id { get; set; }

        [Column("avatar_id", TypeName = "NVARCHAR(64)")]
        public string AvatarId { get; set; }

        [Column("name", TypeName = "NVARCHAR(100)")]
        public string Name { get; set; }

        [Column("username", TypeName = "NVARCHAR(100)")]
        public string Username { get; set; }

        [Column("password", TypeName = "NVARCHAR(100)")]
        public string Password { get; set; }

        [Column("email", TypeName = "NVARCHAR(20)")]
        public string Email { get; set; }

        [Column("title", TypeName = "NVARCHAR(200)")]
        public string Title { get; set; }

        [Column("first_name", TypeName = "NVARCHAR(50)")]
        public string FirstName { get; set; }

        [Column("last_name", TypeName = "NVARCHAR(50)")]
        public string LastName { get; set; }

        [Column("full_name", TypeName = "NVARCHAR(100)")]
        public string FullName => string.Concat(Title, " ", FirstName, " ", LastName);

        [Column("avatar_type", TypeName = "INTEGER")]
        public AvatarType AvatarType { get; set; }

        [Column("accept_terms", TypeName = "BOOLEAN")]
        public bool AcceptTerms { get; set; }

        [Column("verification_token", TypeName = "NVARCHAR(100)")]
        public string VerificationToken { get; set; }

        [Column("verified", TypeName = "DATETIME")]
        public DateTime? Verified { get; set; }

        [Column("is_verified", TypeName = "BOOLEAN")]
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;

        [Column("reset_token", TypeName = "NVARCHAR(100)")]
        public string ResetToken { get; set; }

        [Column("jwt_token", TypeName = "NVARCHAR(100)")]
        public string JwtToken { get; set; }

        [Column("refresh_token", TypeName = "NVARCHAR(100)")]
        public string RefreshToken { get; set; }

        [Column("reset_token_expires", TypeName = "DATETIME")]
        public DateTime? ResetTokenExpires { get; set; }

        [Column("password_reset", TypeName = "DATETIME")]
        public DateTime? PasswordReset { get; set; }

        [Column("last_beamed_in", TypeName = "DATETIME")]
        public DateTime? LastBeamedIn { get; set; }

        [Column("last_beamed_out", TypeName = "DATETIME")]
        public DateTime? LastBeamedOut { get; set; }

        [Column("is_beamed_in", TypeName = "BOOLEAN")]
        public bool IsBeamedIn { get; set; }

        [Column("holon_id", TypeName = "NVARCHAR(64)")]
        public Guid HolonId { get; set; }
        [Column("version", TypeName = "INTEGER")]
        public int Version { get; set; }

        [Column("deleted_date", TypeName = "DATETIME")]
        public DateTime DeletedDate { get; set; }

        [Column("deleted_by_avatar_id", TypeName = "NVARCHAR(64)")]
        public Guid DeletedByAvatarId { get; set; }
    }
}