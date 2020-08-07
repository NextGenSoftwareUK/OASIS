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

CONTRACT oasis : public contract{

public:

    oasis(name self, name code, datastream<const char*> ds);
    ~oasis();

    //constants
    const name OASIS_NAME = name("oasis");
    const uint32_t HOUR_SEC = 3600;
    const uint32_t DAY_SEC = HOUR_SEC * 24; //86400
    const uint32_t WEEK_SEC = DAY_SEC * 7; //604800

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
    ACTION openacct(name eosio_acc);

    //======================== contract tables ========================

    //config singleton
    TABLE config{

        //admin data
        string contract_name;
    string contract_version;
    name admin; //contract admin account

//trackers
    uint32_t total_accounts; //totol registered accounts

    //reserved accounts
    name oasis_account; //eco.oasis

    EOSLIB_SERIALIZE(config, (contract_name)(contract_version)(admin)
        (total_accounts)(oasis_account))
    };
    typedef singleton<name("config"), config> config_singleton;

    //accounts table
    //scope: self
    TABLE account{
        name account_name; //eosio account
    string password;
    string email;
    string title;
    string firstname;
    string lastname;
    string dob;
    string player_address;
    uint32_t karma;
    uint32_t level;

    uint64_t primary_key() const {
        return username.value;
    }
    EOSLIB_SERIALIZE(account, (username)(password)(email)(title)
        (firstname)(lastname)(dob)(player_address)(karma)(level) };

    typedef multi_index<name("accounts"), account> accounts_table;

        };