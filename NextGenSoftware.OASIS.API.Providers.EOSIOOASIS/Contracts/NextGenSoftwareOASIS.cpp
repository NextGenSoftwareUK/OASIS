/*
 * NextGenSoftwareOASIS.cpp - implements NextGenSoftwareOASIS.hpp CRUD methods for Avatar, AvatarDetail, Holon
 * Date: 2022-05-14
 * 
 * NextGenSoftware Copyright (c) 2022
 * 
 */


#include "NextGenSoftwareOASIS.hpp"

using namespace NextGenSoftwareOASIS;

// Inserts an avatar entity into avatars list
[[eosio::action]]
void OASISRepository::CreateAvatar(long entityId, string avatarId, string info) 
{
    Avatars avatarsTable(get_self(), get_first_receiver().value);
    
    // Validating step: check if avatar with current id is exist
    auto existingAvatar = avatarsTable.find(entityId);
    check(existingAvatar == avatarsTable.end(), "Avatar with that EntityId already exists!");

    // Insert avatar entity into avatar table
    avatarsTable.emplace(get_self(), [&](auto& row) 
    {
        row.EntityId = entityId;
        row.AvatarId = avatarId;
        row.Info = info;
        row.IsDeleted = false;
    });

    // Print insertation result
    print("New avatar created, ID: ", entityId);
}

// Reads an avatar entity from avatars list by its entity id
[[eosio::action]]
void OASISRepository::ReadAvatar(long entityId) 
{ 
    Avatars avatarsTable(get_self(), get_first_receiver().value);
    
    auto existingAvatar = avatarsTable.find(entityId);

    // Validation step: is that avatar exist
    check(existingAvatar != avatarsTable.end(), "Avatar with that ID does not exist!");
    // Validation step: check is avatar soft-deleted
    check(existingAvatar.IsDeleted == true, "Avatar soft-deleted!");
    
    const auto& avatar = *existingAvatar;
        
    // Print reading result
    print("Reading avatar executed, readed avatar with ID: ", avatar.EntityId);
}

// Reads all avatars from avatars list
[[eosio::action]]
void OASISRepository::ReadAllAvatars() 
{ 
    // TODO: implement reading of all data
}

// Updates avatar info field by specified avatar entity id
[[eosio::action]]
void OASISRepository::UpdateAvatar(long entityId, string info) 
{
    Avatars avatarsTable(get_self(), get_first_receiver().value);
    auto existingAvatar = avatarsTable.find(entityId);

    // Validation step: is that avatar exist
    check(existingAvatar != avatarsTable.end(), "Avatar with that ID does not exist!");
    // Validation step: check is avatar soft-deleted
    check(existingAvatar.IsDeleted == true, "Avatar soft-deleted!");

    const auto& avatar = *existingAvatar;
    avatarsTable.modify(avatar, get_self(), [&](auto& row) {
        row.Info = info;
    });

    // Print updating result
    print("Avatar updating executed, ID: ", avatar.EntityId);
}

// Removes avatar entity specified by its entity id from avatars list
[[eosio::action]]
void OASISRepository::HardDeleteAvatar(long entityId) 
{
    Avatars avatarsTable(get_self(), get_first_receiver().value);
    auto existingAvatar = avatarsTable.find(entityId);
    
    // Validation step: is that avatar exist
    check(existingAvatar != avatarsTable.end(), "Avatar with that ID does not exist!");
    // Validation step: check is avatar soft-deleted
    check(existingAvatar.IsDeleted == true, "Avatar already soft-deleted!");

    const auto& avatar = *existingAvatar;
    avatarsTable.erase(avatar);

    // Print hard-deleting result
    print("Avatar hard deleted, ID: ", avatar.EntityId);
}

// Sets avatar IsDeleted field value to true specified by entity id
[[eosio::action]]
void OASISRepository::SoftDeleteAvatar(long entityId) 
{
    Avatars avatarsTable(get_self(), get_first_receiver().value);
    auto existingAvatar = avatarsTable.find(entityId);

    // Validation step: is that avatar exist
    check(existingAvatar != avatarsTable.end(), "Avatar with that ID does not exist!");
    // Validation step: check is avatar soft-deleted
    check(existingAvatar.IsDeleted == true, "Avatar already soft-deleted!");

    const auto& avatar = *existingAvatar;
    avatarsTable.modify(avatar, get_self(), [&](auto& row) {
        row.IsDeleted = true;
    });

    // Print soft-deleting result
    print("Avatar soft deleted, ID: ", avatar.EntityId);
}

// Inserts avatar detail entity into avatar details list
[[eosio::action]]
void OASISRepository::CreateAvatarDetail(long entityId, string avatarId, string info) 
{
    AvatarDetails avatarDetailsTable(get_self(), get_first_receiver().value);
    
    // Validating step: check if avatar detail with current id is exist
    auto existingAvatarDetail = avatarDetailsTable.find(entityId);
    check(existingAvatarDetail == avatarDetailsTable.end(), "Avatar detail with that EntityId already exists!");

    // Insert avatar entity into avatar table
    avatarDetailsTable.emplace(get_self(), [&](auto& row) 
    {
        row.EntityId = entityId;
        row.AvatarId = avatarId;
        row.Info = info;
    });

    // Print insertation result
    print("New avatar detail created, ID: ", entityId);
}

// Reads avatar detail entity from avatar details list specified by id
[[eosio::action]]  
void OASISRepository::ReadAvatarDetail(long entityId) 
{
    AvatarDetails avatarDetailsTable(get_self(), get_first_receiver().value);
    
    auto existingAvatarDetail = avatarDetailsTable.find(entityId);

    // Validation step: is that avatar exist
    check(existingAvatarDetail != avatarDetailsTable.end(), "Avatar detail with that ID does not exist!");

    const auto& avatarDetail = *existingAvatar;
        
    // Print reading result
    print("Reading avatar detail executed, readed avatar detail with ID: ", avatarDetail.EntityId);
}

// Read all avatar details from avatar details list
[[eosio::action]]
void OASISRepository::ReadAllAvatarDetails() 
{
    // TODO: Implement reading all avatar details
}

// Inserts an avatar entity into avatars list
[[eosio::action]]
void OASISRepository::CreateHolon(long entityId, string holonId, string info) 
{
    Holons holonsTable(get_self(), get_first_receiver().value);
    
    // Validating step: check if holon with current id is exist
    auto existingHolon = holonsTable.find(entityId);
    check(existingHolon == holonsTable.end(), "Holon with that EntityId already exists!");

    // Insert avatar entity into avatar table
    holonsTable.emplace(get_self(), [&](auto& row) 
    {
        row.EntityId = entityId;
        row.HolonId = holonId;
        row.Info = info;
        row.IsDeleted = false;
    });

    // Print insertation result
    print("New holon created, ID: ", entityId);
}

// Reads holon entity from holons list specified by its id
[[eosio::action]]
void OASISRepository::ReadHolon(long entityId) 
{
    Holons holonsTable(get_self(), get_first_receiver().value);
    auto existingHolon = holonsTable.find(entityId);

    // Validation step: is that holon exist
    check(existingHolon != holonsTable.end(), "Holon with that ID does not exist!");
    // Validation step: check is holon soft-deleted
    check(existingHolon.IsDeleted == true, "Holon already soft-deleted!");
    
    const auto& holon = *existingHolon;
        
    // Print reading result
    print("Reading holon executed, readed holon with ID: ", holon.EntityId);
}

// Read all holons from holons list
[[eosio::action]]
void OASISRepository::ReadAllHolon() 
{
    // Implement reading of all holons
}

// Updates holon info field by specified holon id
[[eosio::action]]
void OASISRepository::UpdateHolon(long entityId, string info) 
{
    Holons holonsTable(get_self(), get_first_receiver().value);
    auto existingHolon = holonsTable.find(entityId);

    // Validation step: is that holon exist
    check(existingHolon != holonsTable.end(), "Holon with that ID does not exist!");
    // Validation step: check is holon soft-deleted
    check(existingHolon.IsDeleted == true, "Holon already soft-deleted!");

    const auto& holon = *existingHolon;
    holonsTable.modify(holon, get_self(), [&](auto& row) {
        row.Info = info;
    });

    // Print updating result
    print("Holon updating executed, ID: ", holon.EntityId);
}

// Removes holon entity specified by its entity id from holons list
[[eosio::action]]
void OASISRepository::HardDeleteHolon(long entityId) 
{
    Holons holonsTable(get_self(), get_first_receiver().value);
    auto existingHolon = holonsTable.find(entityId);

    // Validation step: is that holon exist
    check(existingHolon != holonsTable.end(), "Holon with that ID does not exist!");
    // Validation step: check is holon soft-deleted
    check(existingHolon.IsDeleted == true, "Holon already soft-deleted!");

    const auto& holon = *existingHolon;
    holonsTable.erase(holon);

    // Print hard-deleting result
    print("Holon hard deleted, ID: ", holon.EntityId);
}

// Sets holon IsDeleted field value to true specified by its id
[[eosio::action]]
void OASISRepository::SoftDeleteHolon(long entityId) 
{
    Holons holonsTable(get_self(), get_first_receiver().value);
    auto existingHolon = holonsTable.find(entityId);

    // Validation step: is that holon exist
    check(existingHolon != holonsTable.end(), "Holon with that ID does not exist!");
    // Validation step: check is holon soft-deleted
    check(existingHolon.IsDeleted == true, "Holon already soft-deleted!");

    const auto& holon = *existingHolon;
    holonsTable.modify(holon, get_self(), [&](auto& row) {
        row.IsDeleted = true;
    });

    // Print soft-deleting result
    print("Holon soft deleted, ID: ", holon.EntityId);
}