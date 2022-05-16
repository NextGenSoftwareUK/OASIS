/*
 * NextGenSoftwareOASIS.cpp - implements NextGenSoftwareOASIS.hpp CRUD methods for Avatar, AvatarDetail, Holon
 * Date: 2022-05-14
 * 
 * NextGenSoftware Copyright (c) 2022
 * 
 */


#include "NextGenSoftwareOASIS.hpp"

// Inserts an avatar entity into avatars list
[[eosio::action]]
void NextGenSoftwareOASIS::addavatar(long entityId, string avatarId, string info) 
{
    // Validating step: check if avatar with current id is exist
    for (auto avatarItem : avatarTable) 
    {
        if(avatarItem.entityId == entityId)
            return;
    }

    // Insert avatar entity into avatar table
    avatar avatarEntity;
    avatarEntity.entityId = entityId;
    avatarEntity.isDeleted = false;
    avatarEntity.avatarId = avatarId;
    avatarEntity.info = info;

    avatarTable.push_back(avatarEntity);

    // Print insertation result
    print("New avatar created, ID: ", entityId);
}

// Reads an avatar entity from avatars list by its entity id
[[eosio::action]]
NextGenSoftwareOASIS::avatar NextGenSoftwareOASIS::getavatar(long entityId) 
{
    for (auto avatarItem : avatarTable) 
        if(avatarItem.entityId == entityId && avatarItem.isDeleted == false) 
        {
            // Print reading result
            print("Reading avatar executed, readed avatar with ID: ", avatarItem.entityId);

            return avatarItem;
        }   

    print("Reading avatar executed, avatar not found with ID: ", entityId);

    // if nothing was found
    return avatar{};
}

// Reads all avatars from avatars list
[[eosio::action]]
NextGenSoftwareOASIS::avatar* NextGenSoftwareOASIS::getavatars() 
{ 
    return &avatarTable[0];
}

// Updates avatar info field by specified avatar entity id
[[eosio::action]]
void NextGenSoftwareOASIS::setavatar(long entityId, string info) 
{
    for (auto avatarItem : avatarTable) 
        if(avatarItem.entityId == entityId && avatarItem.isDeleted == false) 
        {
            avatarItem.info = info;

            // Print reading result
            print("Updating completed, Avatar ID: ", avatarItem.entityId);
            return;
        }  
    
    // Print updating result
    print("Avatar not found, updating failed, ID: ", entityId);
}

// Removes avatar entity specified by its entity id from avatars list
[[eosio::action]]
void NextGenSoftwareOASIS::hardavatar(long entityId) 
{
    for (auto avatarItem : avatarTable) 
        if(avatarItem.entityId == entityId && avatarItem.isDeleted == false) 
        {
            // TODO: remove avatar from the list
            // avatarTable.erase(remove(avatarTable.begin(), avatarTable.end(), avatarItem), avatarTable.end());
         
            // Print reading result
            print("Avatar deleted, ID: ", entityId);
            return;
        }  

    // Print hard-deleting result
    print("Avatar not found, deleting failed, ID: ", entityId);
}

// Sets avatar IsDeleted field value to true specified by entity id
[[eosio::action]]
void NextGenSoftwareOASIS::softavatar(long entityId) 
{
    for (auto avatarItem : avatarTable) 
        if(avatarItem.entityId == entityId && avatarItem.isDeleted == false) 
        {
            avatarItem.isDeleted = true;

            // Print reading result
            print("Avatar soft delete failed, ID: ", avatarItem.entityId);
            return;
        }  

    // Print soft-deleting result
    print("Avatar not found, soft deleted failed, ID: ", entityId);
}

// Inserts avatar detail entity into avatar details list
[[eosio::action]]
void NextGenSoftwareOASIS::adddetail(long entityId, string avatarId, string info) 
{
    // Validating step: check if avatar detail with current id is exist
    for (auto detailItem : detailsTable) 
    {
        if(detailItem.entityId == entityId)
            return;
    }

    // Insert avatar detail entity into avatar detail table
    avatardetail detailEntity;
    detailEntity.entityId = entityId;
    detailEntity.avatarId = avatarId;
    detailEntity.info = info;

    detailsTable.push_back(detailEntity);

    // Print insertation result
    print("New avatar detail created, ID: ", entityId);
}

// Reads avatar detail entity from avatar details list specified by id
[[eosio::action]]  
NextGenSoftwareOASIS::avatardetail NextGenSoftwareOASIS::getdetail(long entityId) 
{
    for (auto detailItem : detailsTable) 
        if(detailItem.entityId == entityId) 
        {
            // Print reading result
            print("Reading avatar detail executed, readed avatar detail with ID: ", detailItem.entityId);

            return detailItem;
        }   

    print("Reading avatar detail executed, avatar detail not found with ID: ", entityId);

    // if nothing was found
    return avatardetail{};
}

// Read all avatar details from avatar details list
[[eosio::action]]
NextGenSoftwareOASIS::avatardetail* NextGenSoftwareOASIS::getdetails() 
{
    return &detailsTable[0];
}

// Inserts an avatar entity into avatars list
[[eosio::action]]
void NextGenSoftwareOASIS::addholon(long entityId, string holonId, string info) 
{
    // Validating step: check if holon with current id is exist
    for (auto holonItem : holonsTable) 
    {
        if(holonItem.entityId == entityId)
            return;
    }

    // Insert holon entity into holon table
    holon holonEntity;
    holonEntity.entityId = entityId;
    holonEntity.holonId = holonId;
    holonEntity.isDeleted = false;
    holonEntity.info = info;

    holonsTable.push_back(holonEntity);

    // Print insertation result
    print("New holon created, ID: ", entityId);
}

// Reads holon entity from holons list specified by its id
[[eosio::action]]
NextGenSoftwareOASIS::holon NextGenSoftwareOASIS::getholon(long entityId) 
{
    for (auto holonItem : holonsTable) 
        if(holonItem.entityId == entityId && holonItem.isDeleted == false) 
        {
            // Print reading result
            print("Reading holon executed, readed holon with ID: ", holonItem.entityId);

            return holonItem;
        }   

    print("Reading holon executed, holon not found with ID: ", entityId);

    // if nothing was found
    return holon{};
}

// Read all holons from holons list
[[eosio::action]]
NextGenSoftwareOASIS::holon* NextGenSoftwareOASIS::getholons() 
{
    return &holonsTable[0];
}

// Updates holon info field by specified holon id
[[eosio::action]]
void NextGenSoftwareOASIS::setholon(long entityId, string info) 
{
    for (auto holonItem : holonsTable) 
        if(holonItem.entityId == entityId && holonItem.isDeleted == false) 
        {
            holonItem.info = info;

            // Print reading result
            print("Updating completed, Holon ID: ", holonItem.entityId);
            return;
        }  
    
    // Print updating result
    print("Holon not found, updating failed, ID: ", entityId);
}

// Removes holon entity specified by its entity id from holons list
[[eosio::action]]
void NextGenSoftwareOASIS::hardholon(long entityId) 
{
    for (auto holonItem : holonsTable) 
        if(holonItem.entityId == entityId && holonItem.isDeleted == false) 
        {
            // TODO: remove holon from the list
            // avatarTable.erase(remove(avatarTable.begin(), avatarTable.end(), avatarItem), avatarTable.end());
         
            // Print reading result
            print("Holon deleted, ID: ", entityId);
            return;
        }  

    // Print hard-deleting result
    print("Holon not found, deleting failed, ID: ", entityId);
}

// Sets holon IsDeleted field value to true specified by its id
[[eosio::action]]
void NextGenSoftwareOASIS::softholon(long entityId) 
{
    for (auto holonItem : holonsTable) 
        if(holonItem.entityId == entityId && holonItem.isDeleted == false) 
        {
            holonItem.isDeleted = true;

            // Print reading result
            print("Holon soft delete failed, ID: ", holonItem.entityId);
            return;
        }  

    // Print soft-deleting result
    print("Holon not found, soft deleted failed, ID: ", entityId);
}