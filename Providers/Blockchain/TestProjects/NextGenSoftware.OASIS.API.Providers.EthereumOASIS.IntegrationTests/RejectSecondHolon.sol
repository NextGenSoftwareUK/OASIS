// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

// Test-only ABI-compatible storage: proves real prior commit + later rejection.
contract RejectSecondHolon {
    address public immutable storageOwner = msg.sender;
    uint256 public constant storageProtocolVersion = 2;
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
    uint256 public constant storageProtocolVersion = 1;
}
