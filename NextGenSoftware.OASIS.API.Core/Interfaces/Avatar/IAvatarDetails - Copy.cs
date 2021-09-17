//using System;
//using System.Collections.Generic;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.API.Core.Objects;

//namespace NextGenSoftware.OASIS.API.Core.Interfaces
//{
//    public interface IAvatarDetails : IHolon 
//    {
//        string UmaJson { get; set; }
//        string Image2D { get; set; }
//        string Username { get; set; }
//        string Password { get; set; }
//        string Email { get; set; }
//        string Title { get; set; }
//        string FirstName { get; set; }
//        string LastName { get; set; }
//        ConsoleColor FavouriteColour { get; set; }
//        ConsoleColor STARCLIColour { get; set; }
//        string FullName { get; }
//        DateTime DOB { get; set; }
//        string Address { get; set; }
//        int Karma { get; set; }
//        int Level { get; }
//        int XP { get; set; }
//        IOmiverse Omiverse { get; set; } 
//        List<AvatarGift> Gifts { get; set; }
//        AvatarChakras Chakras { get; set; }
//        AvatarAura Aura { get; set; }
//        AvatarStats Stats { get; set; }
//        List<GeneKey> GeneKeys { get; set; } 
//        HumanDesign HumanDesign { get; set; } 
//        AvatarSkills Skills { get; set; } 
//        AvatarAttributes Attributes { get; set; }
//        AvatarSuperPowers SuperPowers { get; set; } 
//        List<Spell> Spells { get; set; } 
//        List<Achievement> Achievements { get; set; } 
//        List<InventoryItem> Inventory { get; set; } 
//        string Town { get; set; }
//        string County { get; set; }
//        string Country { get; set; }
//        string Postcode { get; set; }
//        string Mobile { get; set; }
//        string Landline { get; set; }
//        EnumValue<AvatarType> AvatarType { get; set; }
//        EnumValue<OASISType> CreatedOASISType { get; set; }
//        bool AcceptTerms { get; set; }
//        public string VerificationToken { get; set; }
//        DateTime? Verified { get; set; }
//        bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
//        string ResetToken { get; set; }
//        string JwtToken { get; set; }
//        string RefreshToken { get; set; }
//        DateTime? ResetTokenExpires { get; set; }
//        DateTime? PasswordReset { get; set; }
//    }
//}