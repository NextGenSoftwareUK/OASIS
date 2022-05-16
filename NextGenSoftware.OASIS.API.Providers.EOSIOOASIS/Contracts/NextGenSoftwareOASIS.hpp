#include <eosio/eosio.hpp>
#include <vector>
#include <string>

using namespace std;
using namespace eosio;

class [[eosio::contract]] NextGenSoftwareOASIS : public contract
{
    public:
        using contract::contract;

        struct avatar
        {
            long entityId;
            string avatarId;
            string info;
            bool isDeleted;
        };

        struct avatardetail
        {
            long entityId;
            string avatarId;
            string info;
        };

        struct holon
        {
            long entityId;
            string holonId;
            string info;
            bool isDeleted;
        };

        vector<avatar> avatarTable;
        vector<avatardetail> detailsTable;
        vector<holon> holonsTable;

        [[eosio::action]]
        void addavatar(long entityId, string avatarId, string info);
        [[eosio::action]]
        avatar getavatar(long entityId);
        [[eosio::action]]
        avatar* getavatars();
        [[eosio::action]]
        void setavatar(long entityId, string info);
        [[eosio::action]]
        void hardavatar(long entityId);
        [[eosio::action]]
        void softavatar(long entityId);

        [[eosio::action]]
        void adddetail(long entityId, string avatarId, string info);
        [[eosio::action]]
        avatardetail getdetail(long entityId);
        [[eosio::action]]
        avatardetail* getdetails();

        [[eosio::action]]
        void addholon(long entityId, string holonId, string info);
        [[eosio::action]]
        holon getholon(long entityId);
        [[eosio::action]]
        holon* getholons();
        [[eosio::action]]
        void setholon(long entityId, string info);
        [[eosio::action]]
        void hardholon(long entityId);
        [[eosio::action]]
        void softholon(long entityId);
};
