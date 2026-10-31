#pragma once

#include <eosio/eosio.hpp>
#include <string>

using eosio::contract;
using eosio::datastream;
using eosio::multi_index;
using eosio::name;
using std::string;

class [[eosio::contract("oasis")]] oasis : public contract
{
public:
    using contract::contract;

    [[eosio::action]] void addholon(int32_t entityId, string holonId, string info);
    [[eosio::action]] void setholon(int32_t entityId, string info);
    [[eosio::action]] void softholon(int32_t entityId);
    [[eosio::action]] void hardholon(int32_t entityId);

    [[eosio::action]] void addavatar(int32_t entityId, string avatarId, string info);
    [[eosio::action]] void setavatar(int32_t entityId, string info);
    [[eosio::action]] void softavatar(int32_t entityId);
    [[eosio::action]] void hardavatar(int32_t entityId);

    [[eosio::action]] void adddetail(int32_t entityId, string holonId, string info);

private:
    struct [[eosio::table("holon")]] holon_row
    {
        int32_t entityId;
        string holonId;
        string info;
        bool isDeleted = false;
        uint64_t primary_key() const { return static_cast<uint32_t>(entityId); }
    };

    struct [[eosio::table("avatar")]] avatar_row
    {
        int32_t entityId;
        string avatarId;
        string info;
        bool isDeleted = false;
        uint64_t primary_key() const { return static_cast<uint32_t>(entityId); }
    };

    struct [[eosio::table("avatardetail")]] avatar_detail_row
    {
        int32_t entityId;
        string avatarId;
        string info;
        uint64_t primary_key() const { return static_cast<uint32_t>(entityId); }
    };

    using holon_table = multi_index<name("holon"), holon_row>;
    using avatar_table = multi_index<name("avatar"), avatar_row>;
    using avatar_detail_table = multi_index<name("avatardetail"), avatar_detail_row>;
};
