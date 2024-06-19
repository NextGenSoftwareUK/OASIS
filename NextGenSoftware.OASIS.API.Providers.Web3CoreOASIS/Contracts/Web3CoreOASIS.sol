// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "./IWeb3CoreOASIS.sol";
import "./EntityOASIS.sol";

contract Web3CoreOASIS is IWeb3CoreOASIS {

    string constant ENTITY_NOT_EXIST = "Entity not exist";
    string constant ENTITY_ALREADY_EXIST = "Entity already exist";
    string constant OWNER_NOT_AUTHORIZED = "Not authorized";

    mapping(uint256 => EntityOASIS) private avatars;
    mapping(uint256 => EntityOASIS) private avatarDetails;
    mapping(uint256 => EntityOASIS) private holons;

    uint256 private totalAvatarsCount;
    uint256 private totalAvatarDetailsCount;
    uint256 private totalHolonsCount;

    address private owner;

    modifier onlyOwner() {
        require(msg.sender == owner, OWNER_NOT_AUTHORIZED);
        _;
    }

    constructor() {
        owner = msg.sender;
        totalAvatarsCount = 0;
        totalAvatarDetailsCount = 0;
        totalHolonsCount = 0;
    }

    function createAvatar(
        uint256 entityId,
        bytes32 avatarId,
        bytes memory info
    ) public onlyOwner returns (uint256) {
        require(avatars[entityId].ExternalId == bytes32(0), ENTITY_ALREADY_EXIST);
        avatars[entityId] = EntityOASIS(entityId, avatarId, info);
        totalAvatarsCount++;
        return totalAvatarsCount;
    }

    function createHolon(
        uint256 entityId,
        bytes32 holonId,
        bytes memory info
    ) public onlyOwner returns (uint256) {
        require(holons[entityId].ExternalId == bytes32(0), ENTITY_ALREADY_EXIST);
        holons[entityId] = EntityOASIS(entityId, holonId, info);
        totalHolonsCount++;
        return totalHolonsCount;
    }

    function createAvatarDetail(
        uint256 entityId,
        bytes32 avatarId,
        bytes memory info
    ) public onlyOwner returns (uint256) {
        require(avatarDetails[entityId].ExternalId == bytes32(0), ENTITY_ALREADY_EXIST);
        avatarDetails[entityId] = EntityOASIS(entityId, avatarId, info);
        totalAvatarDetailsCount++;
        return totalAvatarDetailsCount;
    }

    function updateAvatar(
        uint256 entityId,
        bytes memory info
    ) public onlyOwner returns (bool) {
        require(avatars[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        avatars[entityId].Info = info;
        return true;
    }

    function updateAvatarDetail(
        uint256 entityId,
        bytes memory info
    ) public onlyOwner returns (bool) {
        require(avatarDetails[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        avatarDetails[entityId].Info = info;
        return true;
    }

    function updateHolon(
        uint256 entityId,
        bytes memory info
    ) public onlyOwner returns (bool) {
        require(holons[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        holons[entityId].Info = info;
        return true;
    }

    function deleteAvatar(uint256 entityId) public onlyOwner returns (bool) {
        require(avatars[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        delete avatars[entityId];
        totalAvatarsCount--;
        return true;
    }

    function deleteAvatarDetail(uint256 entityId) public onlyOwner returns (bool) {
        require(avatarDetails[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        delete avatarDetails[entityId];
        totalAvatarDetailsCount--;
        return true;
    }

    function deleteHolon(uint256 entityId) public onlyOwner returns (bool) {
        require(holons[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        delete holons[entityId];
        totalHolonsCount--;
        return true;
    }

    function getAvatarById(uint256 entityId) public view returns (EntityOASIS memory) {
        require(avatars[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        return avatars[entityId];
    }

    function getAvatarDetailById(uint256 entityId) public view returns (EntityOASIS memory) {
        require(avatarDetails[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        return avatarDetails[entityId];
    }

    function getHolonById(uint256 entityId) public view returns (EntityOASIS memory) {
        require(holons[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        return holons[entityId];
    }

    function getAvatarsCount() public view returns (uint256 count) {
        return totalAvatarsCount;
    }

    function getAvatarDetailsCount() public view returns (uint256 count) {
        return totalAvatarDetailsCount;
    }

    function getHolonsCount() public view returns (uint256 count) {
        return totalHolonsCount;
    }
}
