#include "../include/oasis.hpp"

oasis::oasis(name self, name code, datastream<const char *> ds) : contract(self, code, ds) {}
oasis::~oasis() {}

//======================== config actions ========================

ACTION oasis::init(string contract_name, string contract_version, name admin)
{

    //authenticate
    require_auth(get_self());

    //open config singleton
    config_singleton configs(get_self(), get_self().value);

    //validate
    check(!configs.exists(), "config already exists");
    check(is_account(admin), "initial admin account doesn't exist");

    //initialize
    uint32_t total_accounts = 0;
    name oasis_account = name("eco.oasis");

    config initial_conf = {
        contract_name,    //contract_name
        contract_version, //contract_version
        admin,            //admin
        total_accounts,   //total_accounts
        oasis_account,    //oasis_account
    };

    //set new config
    configs.set(initial_conf, get_self());

    //send openacct inline
    //NOTE: requires oasis@eosio.code set to active permission
    action(permission_level{get_self(), name("active")}, get_self(), name("openacct"), make_tuple(get_self())).send();
}

ACTION oasis::setversion(string new_version)
{

    //open configs singleton, get config
    config_singleton configs(get_self(), get_self().value);
    auto conf = configs.get();

    //authenticate
    require_auth(conf.admin);

    //change contract version
    conf.contract_version = new_version;

    //set new config
    configs.set(conf, get_self());
}

//======================== account actions ========================

ACTION oasis::openacct(string userid, name eosio_acc, string providerkey, string password, string email, string title, string firstname,
                       string lastname, string dob, string playeraddr, uint32_t karma)
{

    //open configs singleton, get config
    config_singleton configs(get_self(), get_self().value);
    auto conf = configs.get();

    //authenticate
    require_auth(conf.admin);

    //open accounts table, search for existing account
    accounts_table accounts(get_self(), get_self().value);
    auto acct_itr = accounts.find(eosio_acc.value);

    //validate
    check(acct_itr == accounts.end(), "account already opened");
    check(is_account(eosio_acc), "eosio account does not exist");

    //initialize
    time_point_sec now = time_point_sec(current_time_point());

    //emplace new OASIS account
    //ram payer: contract
    accounts.emplace(get_self(), [&](auto &col) {
        col.userid = userid;
        col.username = eosio_acc;
        col.providerkey = providerkey;
        col.password = password;
        col.email = email;
        col.title = title;
        col.firstname = firstname;
        col.lastname = lastname;
        col.dob = dob;
        col.playeraddr = playeraddr;
        col.karma = karma;
    });

    //update total accounts
    conf.total_accounts += 1;
    configs.set(conf, get_self());
}

ACTION oasis::setstrval(name eosio_acc, string field_name, string new_value)
{

    //open configs singleton, get config
    config_singleton configs(get_self(), get_self().value);
    auto conf = configs.get();

    //authenticate
    require_auth(eosio_acc);

    //get account
    accounts_table accounts(get_self(), get_self().value);
    auto &acct = accounts.get(eosio_acc.value, "account not found");

    //update account password
    accounts.modify(acct, same_payer, [&](auto &col) {
        switch (field_name)
        {
        case "password":
            col.password = new_value;
            break;

        case "email":
            col.email = new_value;
            break;

        case "title":
            col.title = new_value;
            break;

        case "firstname":
            col.firstname = new_value;
            break;

        case "lastname":
            col.lastname = new_value;
            break;

        case "dob":
            col.dob = new_value;
            break;

        case "playeraddr":
            col.playeraddr = new_value;
            break;

        default:
            break;
        }
    });
}

ACTION oasis::setintval(name eosio_acc, string field_name, unit32_t new_value)
{

    //open configs singleton, get config
    config_singleton configs(get_self(), get_self().value);
    auto conf = configs.get();

    //authenticate
    require_auth(eosio_acc);

    //get account
    accounts_table accounts(get_self(), get_self().value);
    auto &acct = accounts.get(eosio_acc.value, "account not found");

    //update account password
    accounts.modify(acct, same_payer, [&](auto &col) {
        switch (field_name)
        {
        case "karma":
            col.karma = new_value;
            break;

        default:
            break;
        }
    });
}