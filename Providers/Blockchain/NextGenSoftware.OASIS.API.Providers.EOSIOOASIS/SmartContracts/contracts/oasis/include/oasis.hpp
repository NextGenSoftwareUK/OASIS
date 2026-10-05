// EOSIO smart contracts for the OASIS.
//
// @author Alexander Bai
// @company
// @contract oasis
// @version v0.1.0

//period names: powerdown, rewards

#include <eosio/eosio.hpp>
#include <eosio/singleton.hpp>
#include <eosio/asset.hpp>

using namespace std;
using namespace eosio;

CONTRACT oasis : public contract
{

public:
    oasis(name self, name code, datastream<const char *> ds);
    ~oasis();

    //constants
    const name OASIS_NAME = name("oasis");
    const uint32_t HOUR_SEC = 3600;
    const uint32_t DAY_SEC = HOUR_SEC * 24; //86400
    const uint32_t WEEK_SEC = DAY_SEC * 7;  //604800

    //======================== config actions ========================

    //initialize the contract
    //auth: self
    ACTION init(string contract_name, string contract_version, name admin);

    //set a new contract version
    //auth: admin
    ACTION setversion(string new_version);

    //======================== account actions ========================

    //open a new OASIS account
    //auth: admin
    ACTION openacct(string userid, name eosio_acc, string providerkey, string password, string email, string title, string firstname,
                    string lastname, string dob, string playeraddr, uint32_t karma);

    //set the account field (string)
    //auth: account
    ACTION setstrval(name eosio_acc, string field_name, string new_value);

    //set the account field (int)
    //auth: account
    ACTION setintval(name eosio_acc, string field_name, unit32_t new_value);

    //======================== contract tables ========================

    //config singleton
    TABLE config
    {

        //admin data
        string contract_name;
        string contract_version;
        name admin; //contract admin account

        //trackers
        uint32_t total_accounts; //totol registered accounts

        //reserved accounts
        name oasis_account; //eco.oasis

        EOSLIB_SERIALIZE(config, (contract_name)(contract_version)(admin)(total_accounts)(oasis_account))
    };
    typedef singleton<name("config"), config> config_singleton;

    //accounts table
    //scope: self
    TABLE account
    {
        string userid;
        name username;      //eosio account
        string providerkey; // unique provider key
        string password;
        string email;
        string title;
        string firstname;
        string lastname;
        string dob;
        string playeraddr;
        uint32_t karma;

        string primary_key() const
        {
            return userid;
        }

        string by_user_name() const
        {
            return username.value;
        }

        string by_provider_key() const
        {
            return providerkey;
        }

    EOSLIB_SERIALIZE(account, (userid)(username)(providerkey)(password)(email)(title)
        (firstname)(lastname)(dob)(playeraddr)(karma)
    };

    typedef multi_index<name("accounts"), account,
                        indexed_by<name("byusername"), const_mem_fun<account, string, &account::by_user_name>>,
                        indexed_by<name("byusername"), const_mem_fun<account, string, &account::by_provider_key>>>
        accounts_table;
};