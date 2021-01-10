pragma solidity >0.5.2;
pragma experimental ABIEncoderV2;

contract OASIS {
    struct account {
        string userid;
        string name;
        string providerkey;
        string password;
        string email;
        string title;
        string firstname;
        string lastname;
        string dob;
        string playeraddr;
        uint32 karma;
    }

    account[] private accounts;
    uint256 public totalAccounts;

    constructor() public {
        totalAccounts = 0;
    }

    function createAccount(
        string memory userid,
        string memory name,
        string memory providerkey,
        string memory password,
        string memory email,
        string memory title,
        string memory firstname,
        string memory lastname,
        string memory dob,
        string memory playeraddr,
        uint32 karma
    ) public returns (uint256) {
        account memory newAccount = account(
            userid,
            name,
            providerkey,
            password,
            email,
            title,
            firstname,
            lastname,
            dob,
            playeraddr,
            karma
        );
        accounts.push(newAccount);
        totalAccounts++;
        return totalAccounts;
    }

    function updateAccount(
        string memory userid,
        string memory name,
        string memory providerkey,
        string memory password,
        string memory email,
        string memory title,
        string memory firstname,
        string memory lastname,
        string memory dob,
        string memory playeraddr,
        uint32 karma
    ) public returns (bool) {
        for (uint256 i = 0; i < totalAccounts; i++) {
            if (compareString(accounts[i].userid, userid)) {
                accounts[i].name = name;
                accounts[i].providerkey = providerkey;
                accounts[i].password = password;
                accounts[i].email = email;
                accounts[i].title = title;
                accounts[i].firstname = firstname;
                accounts[i].lastname = lastname;
                accounts[i].dob = dob;
                accounts[i].playeraddr = playeraddr;
                accounts[i].karma = karma;
                return true;
            }
        }
        return false;
    }

    function deleteAccount(string memory userid) public returns (bool) {
        require(totalAccounts > 0);
        for (uint256 i = 0; i < totalAccounts; i++) {
            if (compareString(accounts[i].userid, userid)) {
                accounts[i] = accounts[totalAccounts - 1];
                delete accounts[totalAccounts - 1];
                totalAccounts--;
                return true;
            }
        }
        return false;
    }

    function getAccountParameter(string memory userid)
        public
        view
        returns (account memory)
    {
        require(totalAccounts > 0);
        for (uint256 i = 0; i < totalAccounts; i++) {
            if (compareString(accounts[i].userid, userid)) {
                return accounts[i];
            }
        }
    }

    function getAccountCount() public view returns (uint256 count) {
        return accounts.length;
    }

    function compareString(string memory first, string memory second)
        public
        pure
        returns (bool)
    {
        return
            bool(
                keccak256(abi.encodePacked(first)) ==
                    keccak256(abi.encodePacked(second))
            );
    }
}
