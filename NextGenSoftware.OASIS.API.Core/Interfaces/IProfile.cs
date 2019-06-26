namespace NextGenSoftware.OASIS.API.Core
{
    public interface IProfile
    {
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string DOB { get; set; }
        string PlayerAddress { get; set; }
        string Karma { get; }
        string Level { get; }

        bool AddKarma(int karmaToAdd);
        bool SubstractKarma(int karmaToRemove);
        bool Save();
    }
}
