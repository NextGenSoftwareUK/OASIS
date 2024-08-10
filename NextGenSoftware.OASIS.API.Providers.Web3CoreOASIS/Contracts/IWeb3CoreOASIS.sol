// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "./EntityOASIS.sol";

interface IWeb3CoreOASIS {

    function createAvatar(uint256 entityId, bytes32 avatarId, bytes memory info) external returns (uint256);
    function createHolon(uint256 entityId, bytes32 holonId, bytes memory info) external returns (uint256);
    function createAvatarDetail(uint256 entityId, bytes32 avatarId, bytes memory info) external returns (uint256);

    function updateAvatar(uint256 entityId, bytes memory info) external returns (bool);
    function updateAvatarDetail(uint256 entityId, bytes memory info) external returns (bool);
    function updateHolon(uint256 entityId, bytes memory info) external returns (bool);

    function deleteAvatar(uint256 entityId) external returns (bool);
    function deleteAvatarDetail(uint256 entityId) external returns (bool);
    function deleteHolon(uint256 entityId) external returns (bool);

    function getAvatarById(uint256 entityId) external view returns (EntityOASIS memory);
    function getAvatarDetailById(uint256 entityId) external view returns (EntityOASIS memory);
    function getHolonById(uint256 entityId) external view returns (EntityOASIS memory);

    function getAvatarsCount() external view returns (uint256);
    function getAvatarDetailsCount() external view returns (uint256);
    function getHolonsCount() external view returns (uint256);
}
