namespace NextGenSoftware.OASIS.API.Core
{
    public interface IProfile : IHolon
    {
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string DOB { get; set; }
        string PlayerAddress { get; set; }
        int Karma { get; }
        int Level { get; }

        bool AddKarma(int karmaToAdd);
        bool SubstractKarma(int karmaToRemove);
        bool Save();
    }
}
