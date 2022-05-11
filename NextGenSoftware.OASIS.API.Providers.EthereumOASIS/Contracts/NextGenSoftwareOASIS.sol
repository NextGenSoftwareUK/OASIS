// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "./Core/Entities/AvatarDetail.sol";
import "./Core/Entities/Avatar.sol";
import "./Core/Entities/Holon.sol";

contract NextGenSoftwareOASIS {
    
    Avatar[] private avatars;
    AvatarDetail[] private avatarDetails;
    Holon[] private holons;
    
    uint256 private totalAvatarsCount;
    uint256 private totalAvatarDetailsCount;
    uint256 private totalHolonsCount;
    
    constructor() {
        totalAvatarsCount = 0;
        totalAvatarDetailsCount = 0;
        totalHolonsCount = 0;
    }

    function CreateAvatar(
        uint256 entityId,
        string memory avatarId,
        string memory info
    ) public returns (uint256) {
        Avatar memory newAvatar = Avatar({
            AvatarId: avatarId,
            EntityId: entityId,
            Info: info
        });
        avatars.push(newAvatar);
        totalAvatarsCount++;
        return totalAvatarsCount;
    }

    function CreateHolon(
        uint256 entityId,
        string memory holonId,
        string memory info
    ) public returns (uint256) {
        Holon memory newHolon = Holon(
        {
            EntityId: entityId,
            HolonId: holonId,
            Info: info
        });
        holons.push(newHolon);
        totalHolonsCount++;
        return totalHolonsCount;
    }

    function CreateAvatarDetail(
        uint256 entityId,
        string memory avatarId,
        string memory info
    ) public returns (uint256) {
        AvatarDetail memory newAvatarDetail = AvatarDetail(
        {
            AvatarId: avatarId,
            EntityId: entityId,
            Info: info
        });
        avatarDetails.push(newAvatarDetail);
        totalAvatarDetailsCount++;
        return totalAvatarDetailsCount;
    }

    function UpdateAvatar(
        uint256 entityId,
        string memory info
    ) public returns (bool) {
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].EntityId == entityId) {
                avatars[i].Info = info;
                return true;
            }
        }
        return false;
    }

    function UpdateAvatarDetail(
        uint256 entityId,
        string memory info
    ) public returns (bool) {
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].EntityId == entityId) {
                avatarDetails[i].Info = info;
                return true;
            }
        }
        return false;
    }

    function UpdateHolon(
        uint256 entityId,
        string memory info
    ) public returns (bool) {
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (holons[i].EntityId == entityId) {
                holons[i].Info = info;
                return true;
            }
        }
        return false;
    }

    function DeleteAvatar(uint256 entityId) public returns (bool) {
        require(totalAvatarsCount > 0);
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].EntityId == entityId) {
                avatars[i] = avatars[totalAvatarsCount - 1];
                delete avatars[totalAvatarsCount - 1];
                totalAvatarsCount--;
                return true;
            }
        }
        return false;
    }
    
    function DeleteAvatarDetail(uint256 entityId) public returns (bool) {
        require(totalAvatarDetailsCount > 0);
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].EntityId == entityId) {
                avatarDetails[i] = avatarDetails[totalAvatarDetailsCount - 1];
                delete avatarDetails[totalAvatarDetailsCount - 1];
                totalAvatarDetailsCount--;
                return true;
            }
        }
        return false;
    }
    
    function DeleteHolon(uint256 entityId) public returns (bool) {
        require(totalHolonsCount > 0);
        for (uint256 i = 0; i < totalHolonsCount; i++) {
            if (holons[i].EntityId == entityId) {
                holons[i] = holons[totalHolonsCount - 1];
                delete holons[totalHolonsCount - 1];
                totalHolonsCount--;
                return true;
            }
        }
        return false;
    }
    
    function GetAvatarById(uint256 entityId) public view returns (Avatar memory)
    {
        Avatar memory avatar;
        require(totalAvatarsCount > 0);
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].EntityId == entityId) {
                return avatars[i];
            }
        }
        return avatar;
    }

    function GetAvatarDetailById(uint256 entityId) public view returns (AvatarDetail memory)
    {
        AvatarDetail memory avatarDetail;
        require(totalAvatarDetailsCount > 0);
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].EntityId == entityId) {
                return avatarDetails[i];
            }
        }
        return avatarDetail;
    }

    function GetHolonById(uint256 entityId) public view returns (Holon memory)
    {
        Holon memory holon;
        require(totalHolonsCount > 0);
        for (uint256 i = 0; i < totalHolonsCount; i++) {
            if (holons[i].EntityId == entityId) {
                return holons[i];
            }
        }
        return holon;
    }
    
    function GetAvatarsCount() public view returns (uint256 count) {
        return avatars.length;
    }

    function GetAvatarDetailsCount() public view returns (uint256 count) {
        return avatarDetails.length;
    }

    function GetHolonsCount() public view returns (uint256 count) {
        return holons.length;
    }
}