#include "NextGenSoftwareOASIS.hpp"

// Inserts an avatar entity into avatars list
[[eosio::action]]
void NextGenSoftwareOASIS::addavatar(long entityId, std::string avatarId, std::string info) 
{
    eosio::print("Avatar creating started...");

    // Insert new avatar
    _avtrs.emplace(get_self(), [&](auto& a) {
        a.key = _avtrs.available_primary_key();
        a.entityId = entityId;
        a.avatarId = avatarId;
        a.info = info;
        a.isDeleted = false;
    });

    // Print insertation result
    eosio::print("New avatar created, ID: ", entityId);
}

// Updates avatar info field by specified avatar entity id
[[eosio::action]]
void NextGenSoftwareOASIS::setavatar(long entityId, std::string info) 
{
    eosio::print("Avatar updating started...");

    std::vector<uint64_t> keysForModify;
    for(auto& item : _avtrs) {
        if (item.entityId == entityId && item.isDeleted == false) {
            keysForModify.push_back(item.key);   
        }
    }
    
    for (uint64_t key : keysForModify) {
        auto itr = _avtrs.find(key);
        if (itr != _avtrs.end()) {
            _avtrs.modify(itr, get_self(), [&](auto& a) {
                a.info = info;
            });
        }
    }
    
    // Print updating result
    eosio::print("Avatar updated, ID: ", entityId);
}

// Removes avatar entity specified by its entity id from avatars list
[[eosio::action]]
void NextGenSoftwareOASIS::hardavatar(long entityId) 
{
    std::vector<uint64_t> keysForDeletion;

    for(auto& item : _avtrs) {
        if (item.entityId == entityId && item.isDeleted == false) {
            keysForDeletion.push_back(item.key);   
        }
    }
    
    for (uint64_t key : keysForDeletion) {
        auto itr = _avtrs.find(key);
        if (itr != _avtrs.end()) {
            _avtrs.erase(itr);
        }
    }
    
    // Print hard-deleting result
    eosio::print("Avatar hard deleted, ID: ", entityId);
}

// Sets avatar IsDeleted field value to true specified by entity id
[[eosio::action]]
void NextGenSoftwareOASIS::softavatar(long entityId) 
{
    std::vector<uint64_t> keysForModify;
    for(auto& item : _avtrs) {
        if (item.entityId == entityId && item.isDeleted == false) {
            keysForModify.push_back(item.key);   
        }
    }
    
    for (uint64_t key : keysForModify) {
        auto itr = _avtrs.find(key);
        if (itr != _avtrs.end()) {
            _avtrs.modify(itr, get_self(), [&](auto& a) {
                a.isDeleted = true;
            });
        }
    }

    // Print soft-deleting result
    eosio::print("Avatar soft deleted (deactivated), ID: ", entityId);
}

// Inserts avatar detail entity into avatar details list
[[eosio::action]]
void NextGenSoftwareOASIS::adddetail(long entityId, std::string avatarId, std::string info) 
{
    eosio::print("Avatar detail creating started...");

    // Insert avatar detail entity into avatar detail table
    _dtls.emplace(get_self(), [&](auto& a) {
        a.key = _dtls.available_primary_key();
        a.entityId = entityId;
        a.avatarId = avatarId;
        a.info = info;
    });

    // Print insertation result
    eosio::print("New avatar detail created, ID: ", entityId);
}

// Inserts an avatar entity into avatars list
[[eosio::action]]
void NextGenSoftwareOASIS::addholon(long entityId, std::string holonId, std::string info) 
{
    eosio::print("Holon creating started...");

    // Insert holon entity into holon table
    _hlns.emplace(get_self(), [&](auto& h) {
        h.key = _hlns.available_primary_key();
        h.entityId = entityId;
        h.holonId = holonId;
        h.info = info;
        h.isDeleted = false;
    });

    // Print insertation result
    eosio::print("New holon created, ID: ", entityId);
}

// Updates holon info field by specified holon id
[[eosio::action]]
void NextGenSoftwareOASIS::setholon(long entityId, std::string info) 
{
    eosio::print("Holon updating started...");

    std::vector<uint64_t> keysForModify;
    for(auto& item : _hlns) {
        if (item.entityId == entityId && item.isDeleted == false) {
            keysForModify.push_back(item.key);   
        }
    }
    
    for (uint64_t key : keysForModify) {
        auto itr = _hlns.find(key);
        if (itr != _hlns.end()) {
            _hlns.modify(itr, get_self(), [&](auto& h) {
                h.info = info;
            });
        }
    }
    
    // Print updating result
    eosio::print("Holon updated, ID: ", entityId);
}

// Removes holon entity specified by its entity id from holons list
[[eosio::action]]
void NextGenSoftwareOASIS::hardholon(long entityId) 
{
    std::vector<uint64_t> keysForDeletion;

    for(auto& item : _hlns) {
        if (item.entityId == entityId && item.isDeleted == false) {
            keysForDeletion.push_back(item.key);   
        }
    }
    
    for (uint64_t key : keysForDeletion) {
        auto itr = _hlns.find(key);
        if (itr != _hlns.end()) {
            _hlns.erase(itr);
        }
    }

    // Print hard-deleting result
    eosio::print("Holon hard-deleted, ID: ", entityId);
}

// Sets holon IsDeleted field value to true specified by its id
[[eosio::action]]
void NextGenSoftwareOASIS::softholon(long entityId) 
{
    std::vector<uint64_t> keysForModify;
    for(auto& item : _hlns) {
        if (item.entityId == entityId && item.isDeleted == false) {
            keysForModify.push_back(item.key);   
        }
    }
    
    for (uint64_t key : keysForModify) {
        auto itr = _hlns.find(key);
        if (itr != _hlns.end()) {
            _hlns.modify(itr, get_self(), [&](auto& h) {
                h.isDeleted = true;
            });
        }
    }

    // Print soft-deleting result
    eosio::print("Holon soft-deleted, ID: ", entityId);
}

EOSIO_DISPATCH( NextGenSoftwareOASIS, (softholon)(hardholon)(setholon)(addholon)(adddetail)(softavatar)(hardavatar)(setavatar)(addavatar))