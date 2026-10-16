// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

// Test-only ABI-compatible storage: proves real prior commit + later rejection.
contract RejectSecondHolon {
    address public immutable storageOwner = msg.sender;
    uint256 public constant storageProtocolVersion = 3;
    struct Holon { uint256 EntityId; string HolonId; string Info; }
    mapping(uint256 => Holon) private records;
    uint256 private writes;
    function CreateHolon(uint256 entityId, string memory holonId, string memory info) external returns (uint256) {
        require(writes == 0, "owned fixture rejects second holon");
        records[entityId] = Holon(entityId, holonId, info);
        return ++writes;
    }
    function GetHolonById(uint256 entityId) external view returns (Holon memory) { return records[entityId]; }
}

contract LegacyStorageProtocolFixture {
    address public immutable storageOwner = msg.sender;
}

contract UnsupportedStorageProtocolFixture {
    address public immutable storageOwner = msg.sender;
    uint256 public constant storageProtocolVersion = 2;
}

contract OwnedErc20Fixture {
    uint8 public immutable decimals;
    bool public immutable noOp;
    uint256 public totalSupply;
    mapping(address => uint256) public balanceOf;
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
    function burn(uint256 amount) external {
        if (noOp) return;
        require(balanceOf[msg.sender] >= amount, "invalid burn");
        balanceOf[msg.sender] -= amount;
        totalSupply -= amount;
        emit Transfer(msg.sender, address(0), amount);
    }
}
