// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

// Test-only ABI-compatible storage: proves real prior commit + later rejection.
contract RejectSecondHolon {
    address public immutable storageOwner = msg.sender;
    uint256 public constant storageProtocolVersion = 3;
    uint256 public constant checkedStorageProtocolVersion = 1;
    struct Holon { uint256 EntityId; string HolonId; string Info; }
    mapping(uint256 => Holon) private records;
    uint256 private writes;
    function CreateHolon(uint256 entityId, string memory holonId, string memory info) external returns (uint256) {
        require(writes == 0, "owned fixture rejects second holon");
        records[entityId] = Holon(entityId, holonId, info);
        return ++writes;
    }
    function GetHolonById(uint256 entityId) external view returns (Holon memory) { return records[entityId]; }
    function CreateHolonChecked(uint256 entityId,string memory identity,bool expectedPresent,bytes32 expectedInfoHash,string memory info) external returns(uint256) {
        require(msg.sender==storageOwner,"OASIS storage owner required");
        Holon memory prior=records[entityId];
        bool present=bytes(prior.HolonId).length!=0;
        require(present==expectedPresent,"OASIS presence changed");
        if(present){
            require(keccak256(bytes(prior.HolonId))==keccak256(bytes(identity)),"OASIS identity collision");
            require(keccak256(bytes(prior.Info))==expectedInfoHash,"OASIS record changed");
        }else require(expectedInfoHash==bytes32(0),"OASIS absent hash required");
        require(writes==0,"owned fixture rejects second holon");
        records[entityId]=Holon(entityId,identity,info);return ++writes;
    }
}

contract Protocol3WithoutCheckedStorageFixture {
    address public immutable storageOwner=msg.sender;
    uint256 public constant storageProtocolVersion=3;
}

contract LegacyStorageProtocolFixture {
    address public immutable storageOwner = msg.sender;
}

contract UnsupportedStorageProtocolFixture {
    address public immutable storageOwner = msg.sender;
    uint256 public constant storageProtocolVersion = 2;
}

contract OwnedErc20Fixture {
    address public immutable minter = msg.sender;
    uint8 public immutable decimals;
    bool public immutable noOp;
    uint256 public totalSupply;
    mapping(address => uint256) public balanceOf;
    mapping(address => mapping(address => uint256)) public allowance;
    event Transfer(address indexed from, address indexed to, uint256 value);
    constructor(uint8 precision, bool noOpMutation) {
        decimals = precision;
        noOp = noOpMutation;
        totalSupply = 1000000 * (10 ** uint256(precision));
        balanceOf[msg.sender] = totalSupply;
    }
    function transfer(address to, uint256 amount) external returns (bool) {
        if (noOp) return false;
        require(to != address(0) && balanceOf[msg.sender] >= amount, "invalid transfer");
        balanceOf[msg.sender] -= amount;
        balanceOf[to] += amount;
        emit Transfer(msg.sender, to, amount);
        return true;
    }
    function approve(address spender, uint256 amount) external returns (bool) {
        allowance[msg.sender][spender] = amount;
        return true;
    }
    function transferFrom(address from, address to, uint256 amount) external returns (bool) {
        if (noOp) return false;
        require(allowance[from][msg.sender] >= amount, "Insufficient allowance");
        require(to != address(0) && balanceOf[from] >= amount, "Invalid transferFrom");
        allowance[from][msg.sender] -= amount;
        balanceOf[from] -= amount;
        balanceOf[to] += amount;
        emit Transfer(from, to, amount);
        return true;
    }
    function burn(uint256 amount) external {
        if (noOp) return;
        require(balanceOf[msg.sender] >= amount, "invalid burn");
        balanceOf[msg.sender] -= amount;
        totalSupply -= amount;
        emit Transfer(msg.sender, address(0), amount);
    }
    function mint(address to, uint256 amount) external {
        require(msg.sender == minter && to != address(0), "unauthorized mint");
        if (noOp) return;
        balanceOf[to] += amount;
        totalSupply += amount;
        emit Transfer(address(0), to, amount);
    }
}
