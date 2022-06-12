using System;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.Models
{
    public class SolanaAvatarDetailDto
    {
        public Guid Id { get; set; }
        public string Address { get; set; }
        public string Country { get; set; }
        public string Mobile { get; set; }
        public int Karma { get; set; }
        public int Xp { get; set; }
    }
}