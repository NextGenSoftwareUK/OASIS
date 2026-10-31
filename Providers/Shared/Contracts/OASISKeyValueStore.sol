// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

// Shared storage primitive for SDK-backed EVM-compatible providers.
// On-chain values are public. Callers must not persist plaintext secrets.
contract OASISKeyValueStore {
    address public immutable owner;
    uint256 public revision;
    mapping(bytes32 => string) private values;
    mapping(bytes32 => uint256) private positions;
    string[] private keys;

    constructor() { owner = msg.sender; }
    modifier onlyOwner() { require(msg.sender == owner, "Not storage owner"); _; }

    function get(string calldata key) external view returns (bool exists, string memory value) {
        bytes32 id = keccak256(bytes(key));
        return (positions[id] != 0, values[id]);
    }

    function put(string calldata key, string calldata value) external onlyOwner {
        require(bytes(key).length > 0, "Empty key");
        bytes32 id = keccak256(bytes(key));
        if (positions[id] == 0) {
            keys.push(key);
            positions[id] = keys.length;
        }
        values[id] = value;
        revision++;
    }

    function remove(string calldata key) external onlyOwner {
        bytes32 id = keccak256(bytes(key));
        uint256 position = positions[id];
        if (position == 0) return;
        uint256 last = keys.length - 1;
        uint256 index = position - 1;
        if (index != last) {
            string memory moved = keys[last];
            keys[index] = moved;
            positions[keccak256(bytes(moved))] = position;
        }
        keys.pop();
        delete positions[id];
        delete values[id];
        revision++;
    }

    // Enumerate bounded pages, then filter/sort at the backend. A caller must
    // retain the same revision for every page; mutations invalidate the scan.
    function keyPage(uint256 expectedRevision, uint256 cursor, uint256 limit)
        external view returns (string[] memory page, uint256 next, uint256 total)
    {
        require(expectedRevision == revision, "Storage changed during scan");
        require(limit > 0 && limit <= 256, "Invalid page limit");
        require(cursor <= keys.length, "Invalid cursor");
        uint256 count = keys.length - cursor;
        if (count > limit) count = limit;
        page = new string[](count);
        for (uint256 i = 0; i < count; i++) page[i] = keys[cursor + i];
        return (page, cursor + count, keys.length);
    }
}
