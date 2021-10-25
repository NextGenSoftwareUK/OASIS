// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "..\Enums\AvatarType.sol";
import "..\Enums\ProviderType.sol";
import "..\Common\Achievement.sol";
import "..\Common\AvatarAttributes.sol";
import "..\Common\AvatarAura.sol";

struct AvatarDetail {
    string AvatarId;
    string Title;
    string FirstName;
    string LastName;
    string FullName;
    string Username;
    string Email;
    Achievement[] Achievements;
    string Address;
    AvatarAttributes Attributes;
    AvatarAura Aura;
    AvatarType AvatarType;
    AvatarChakras Chakras;
    string Country;
    string County;
    OASISType CreatedOASISType;
    mapping(DimensionLevel => string) DimensionLevelIds;
    uint256 DOB;
    ConsoleColor FavouriteColour;
    GeneKey GeneKeys;
    AvatarGift Gifts;
    HeartRateEntry HeartRateData;
    HumanDesign HumanDesign;
    string Image2D;
    InventoryItem[] Inventory;
    int Karma;
    KarmaAkashicRecord[] KarmaAkashicRecords;
    string Landline;
    int Level;
    string Mobile;
    string Model3D;
    IOmiverse Omiverse;
    string Postcode;
    mapping(ProviderType => string) ProviderPrivateKey;
    mapping(ProviderType => string) ProviderPublicKey;
    mapping(ProviderType => string) ProviderUsername;
    mapping(ProviderType => string) ProviderWalletAddress;
    AvatarSkills Skills;
    Spell[] Spells;
    ConsoleColor STARCLIColour;
    AvatarStats Stats;
    AvatarSuperPowers SuperPowers;
    string Town;
    string UmaJson;
    int XP;
}
