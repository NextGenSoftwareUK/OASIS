#pragma once

#include <eosio/eosio.hpp>

class [[eosio::contract]] NextGenSoftwareOASIS : public eosio::contract
{
    public:
        NextGenSoftwareOASIS( eosio::name receiver, eosio::name code, eosio::datastream<const char*> ds ): eosio::contract(receiver, code, ds),  _avtrs(receiver, code.value), _hlns(receiver, code.value), _dtls(receiver, code.value)
        {}
        
        [[eosio::action]]
        void addavatar(long entityId, std::string avatarId, std::string info);
        [[eosio::action]]
        void setavatar(long entityId, std::string info);
        [[eosio::action]]
        void hardavatar(long entityId);
        [[eosio::action]]
        void softavatar(long entityId);

        [[eosio::action]]
        void adddetail(long entityId, std::string avatarId, std::string info);

        [[eosio::action]]
        void addholon(long entityId, std::string holonId, std::string info);
        [[eosio::action]]
        void setholon(long entityId, std::string info);
        [[eosio::action]]
        void hardholon(long entityId);
        [[eosio::action]]
        void softholon(long entityId);
        
        struct [[eosio::table]] avatar
        {
            uint64_t key;
            uint64_t entityId;
            std::string avatarId;
            std::string info;
            bool isDeleted;
            
            uint64_t primary_key() const { return key; }
            uint64_t by_entityId() const { return entityId; }
        };
        typedef eosio::multi_index<"avatar"_n, avatar, eosio::indexed_by<"avatarid"_n, eosio::const_mem_fun<avatar, uint64_t, &avatar::by_entityId>>> avtrstable;

        struct [[eosio::table]] avatardetail
        {
            uint64_t key;
            uint64_t entityId;
            std::string avatarId;
            std::string info;
            
            uint64_t primary_key() const { return key; }
            uint64_t by_entityId() const { return entityId; }
        };
        typedef eosio::multi_index<"avatardetail"_n, avatardetail, eosio::indexed_by<"detailid"_n, eosio::const_mem_fun<avatardetail, uint64_t, &avatardetail::by_entityId>>> dtlstable;

        struct [[eosio::table]] holon
        {
            uint64_t key;
            uint64_t entityId;
            std::string holonId;
            std::string info;
            bool isDeleted;
            
            uint64_t primary_key() const { return key; }
            uint64_t by_entityId() const { return entityId; }
        };
        typedef eosio::multi_index<"holon"_n, holon, eosio::indexed_by<"holonid"_n, eosio::const_mem_fun<holon, uint64_t, &holon::by_entityId>>> hlnstable;
        
        avtrstable _avtrs;
        dtlstable _dtls;
        hlnstable _hlns;
};
