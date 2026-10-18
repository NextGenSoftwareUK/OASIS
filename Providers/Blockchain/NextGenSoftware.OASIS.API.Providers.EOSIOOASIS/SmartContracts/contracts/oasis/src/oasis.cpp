#include "../include/oasis.hpp"

void oasis::addholon(int32_t entityId, string holonId, string info)
{
    require_auth(get_self());
    holon_table rows(get_self(), get_self().value);
    eosio::check(rows.find(static_cast<uint32_t>(entityId)) == rows.end(), "holon already exists");
    rows.emplace(get_self(), [&](auto &row) {
        row.entityId = entityId;
        row.holonId = holonId;
        row.info = info;
    });
}

void oasis::setholon(int32_t entityId, string info)
{
    require_auth(get_self());
    holon_table rows(get_self(), get_self().value);
    auto row = rows.require_find(static_cast<uint32_t>(entityId), "holon not found");
    rows.modify(row, eosio::same_payer, [&](auto &value) { value.info = info; });
}

void oasis::softholon(int32_t entityId)
{
    require_auth(get_self());
    holon_table rows(get_self(), get_self().value);
    auto row = rows.require_find(static_cast<uint32_t>(entityId), "holon not found");
    rows.modify(row, eosio::same_payer, [&](auto &value) { value.isDeleted = true; });
}

void oasis::hardholon(int32_t entityId)
{
    require_auth(get_self());
    holon_table rows(get_self(), get_self().value);
    rows.erase(rows.require_find(static_cast<uint32_t>(entityId), "holon not found"));
}

void oasis::addavatar(int32_t entityId, string avatarId, string info)
{
    require_auth(get_self());
    avatar_table rows(get_self(), get_self().value);
    eosio::check(rows.find(static_cast<uint32_t>(entityId)) == rows.end(), "avatar already exists");
    rows.emplace(get_self(), [&](auto &row) {
        row.entityId = entityId;
        row.avatarId = avatarId;
        row.info = info;
    });
}

void oasis::setavatar(int32_t entityId, string info)
{
    require_auth(get_self());
    avatar_table rows(get_self(), get_self().value);
    auto row = rows.require_find(static_cast<uint32_t>(entityId), "avatar not found");
    rows.modify(row, eosio::same_payer, [&](auto &value) { value.info = info; });
}

void oasis::softavatar(int32_t entityId)
{
    require_auth(get_self());
    avatar_table rows(get_self(), get_self().value);
    auto row = rows.require_find(static_cast<uint32_t>(entityId), "avatar not found");
    rows.modify(row, eosio::same_payer, [&](auto &value) { value.isDeleted = true; });
}

void oasis::hardavatar(int32_t entityId)
{
    require_auth(get_self());
    avatar_table rows(get_self(), get_self().value);
    rows.erase(rows.require_find(static_cast<uint32_t>(entityId), "avatar not found"));
}

void oasis::adddetail(int32_t entityId, string holonId, string info)
{
    require_auth(get_self());
    avatar_detail_table rows(get_self(), get_self().value);
    eosio::check(rows.find(static_cast<uint32_t>(entityId)) == rows.end(), "avatar detail already exists");
    rows.emplace(get_self(), [&](auto &row) {
        row.entityId = entityId;
        row.avatarId = holonId;
        row.info = info;
    });
}
