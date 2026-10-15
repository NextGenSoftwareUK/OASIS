// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "./Core/Entities/AvatarDetail.sol";
import "./Core/Entities/Avatar.sol";
import "./Core/Entities/Holon.sol";

contract NextGenSoftwareOASIS {
    address public immutable storageOwner;
    uint256 public constant storageProtocolVersion = 2;

    modifier onlyStorageOwner() {
        require(msg.sender == storageOwner, "OASIS storage owner required");
        _;
    }
    
    Avatar[] private avatars;
    AvatarDetail[] private avatarDetails;
    Holon[] private holons;
    
    uint256 private totalAvatarsCount;
    uint256 private totalAvatarDetailsCount;
    uint256 private totalHolonsCount;
    
    constructor() {
        storageOwner = msg.sender;
        totalAvatarsCount = 0;
        totalAvatarDetailsCount = 0;
        totalHolonsCount = 0;
    }

    function CreateAvatar(
        uint256 entityId,
        string memory avatarId,
        string memory info
    ) public onlyStorageOwner returns (uint256) {
        // Provider Save operations use this ABI for create and update. Keep one row per identity.
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].EntityId == entityId) {
                require(keccak256(bytes(avatars[i].AvatarId)) == keccak256(bytes(avatarId)), "OASIS identity collision");
                avatars[i].Info = info;
                return i + 1;
            }
        }
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
    ) public onlyStorageOwner returns (uint256) {
        for (uint256 i = 0; i < totalHolonsCount; i++) {
            if (holons[i].EntityId == entityId) {
                require(keccak256(bytes(holons[i].HolonId)) == keccak256(bytes(holonId)), "OASIS identity collision");
                holons[i].Info = info;
                return i + 1;
            }
        }
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
    ) public onlyStorageOwner returns (uint256) {
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].EntityId == entityId) {
                require(keccak256(bytes(avatarDetails[i].AvatarId)) == keccak256(bytes(avatarId)), "OASIS identity collision");
                avatarDetails[i].Info = info;
                return i + 1;
            }
        }
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
    ) public onlyStorageOwner returns (bool) {
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].EntityId == entityId) {
                avatars[i].Info = info;
                return true;
            }
        }
        revert("OASIS record not found");
    }

    function UpdateAvatarDetail(
        uint256 entityId,
        string memory info
    ) public onlyStorageOwner returns (bool) {
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].EntityId == entityId) {
                avatarDetails[i].Info = info;
                return true;
            }
        }
        revert("OASIS record not found");
    }

    function UpdateHolon(
        uint256 entityId,
        string memory info
    ) public onlyStorageOwner returns (bool) {
        for (uint256 i = 0; i < totalHolonsCount; i++) {
            if (holons[i].EntityId == entityId) {
                holons[i].Info = info;
                return true;
            }
        }
        revert("OASIS record not found");
    }

    // Validate the full public identity in the SAME transaction as deletion.
    // A client-side read check cannot protect against a concurrent delete/reinsert.
    function DeleteAvatarByIdentity(uint256 entityId, string memory avatarId) public onlyStorageOwner returns (bool) {
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].EntityId == entityId) {
                require(keccak256(bytes(avatars[i].AvatarId)) == keccak256(bytes(avatarId)), "OASIS identity collision");
                return DeleteAvatar(entityId);
            }
        }
        revert("OASIS record not found");
    }

    function DeleteAvatarDetailByIdentity(uint256 entityId, string memory avatarId) public onlyStorageOwner returns (bool) {
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].EntityId == entityId) {
                require(keccak256(bytes(avatarDetails[i].AvatarId)) == keccak256(bytes(avatarId)), "OASIS identity collision");
                return DeleteAvatarDetail(entityId);
            }
        }
        revert("OASIS record not found");
    }

    function DeleteHolonByIdentity(uint256 entityId, string memory holonId) public onlyStorageOwner returns (bool) {
        for (uint256 i = 0; i < totalHolonsCount; i++) {
            if (holons[i].EntityId == entityId) {
                require(keccak256(bytes(holons[i].HolonId)) == keccak256(bytes(holonId)), "OASIS identity collision");
                return DeleteHolon(entityId);
            }
        }
        revert("OASIS record not found");
    }

    function DeleteAvatar(uint256 entityId) public onlyStorageOwner returns (bool) {
        require(totalAvatarsCount > 0);
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].EntityId == entityId) {
                avatars[i] = avatars[totalAvatarsCount - 1];
                avatars.pop();
                totalAvatarsCount--;
                return true;
            }
        }
        revert("OASIS record not found");
    }
    
    function DeleteAvatarDetail(uint256 entityId) public onlyStorageOwner returns (bool) {
        require(totalAvatarDetailsCount > 0);
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].EntityId == entityId) {
                avatarDetails[i] = avatarDetails[totalAvatarDetailsCount - 1];
                avatarDetails.pop();
                totalAvatarDetailsCount--;
                return true;
            }
        }
        revert("OASIS record not found");
    }
    
    function DeleteHolon(uint256 entityId) public onlyStorageOwner returns (bool) {
        require(totalHolonsCount > 0);
        for (uint256 i = 0; i < totalHolonsCount; i++) {
            if (holons[i].EntityId == entityId) {
                holons[i] = holons[totalHolonsCount - 1];
                holons.pop();
                totalHolonsCount--;
                return true;
            }
        }
        revert("OASIS record not found");
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

    function GetAvatarDetailAt(uint256 index) public view returns (AvatarDetail memory) {
        return avatarDetails[index];
    }

    function GetHolonsCount() public view returns (uint256 count) {
        return holons.length;
    }
}
