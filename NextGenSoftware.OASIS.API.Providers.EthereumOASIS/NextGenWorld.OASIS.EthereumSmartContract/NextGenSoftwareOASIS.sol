// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "./Core\Entities\AvatarDetail.sol";
import "./Core\Entities\Avatar.sol";
import "./Core\Entities\Holon.sol";
import "./Core\Enums\AvatarType.sol";

contract NextGenSoftwareOASIS {
    
    Avatar[] private avatars;
    AvatarDetail[] private avatarDetails;
    Holon[] private holons;
    
    uint256 private totalAvatarsCount;
    uint256 private totalAvatarDetailsCount;
    uint256 private totalHolonsCount;
    
    constructor() public {
        totalAccountsCount = 0;
        totalAvatarDetailsCount = 0;
        totalHolonsCount = 0;
    }

    function SaveAvatar(
        string avatarId,
        string title,
        string firstName,
        string lastName,
        string fullName,
        string username,
        string email,
        string password,
        AvatarType avatarType,
        bool acceptTerms,
        bool isVerified,
        string jwtToken,
        uint256 passwordReset,
        string refreshToken,
        string resetToken,
        uint256 resetTokenExpires,
        string verificationToken,
        uint256 verified,
        uint256 lastBeamedIn,
        uint256 lastBeamedOut,
        bool isBeamedIn,
        string image2D,
        int karma,
        int level,
        int xp
    ) public returns (uint256) {
        Avatar memory newAvatar = Avatar(
            avatarId,
            title,
            firstName,
            lastName,
            fullName,
            username,
            email,
            password,
            avatarType,
            acceptTerms,
            isVerified,
            jwtToken,
            passwordReset,
            refreshToken,
            resetToken,
            resetTokenExpires,
            verificationToken,
            verified,
            lastBeamedIn,
            lastBeamedOut,
            isBeamedIn,
            image2D,
            karma,
            level,
            xp
        );
        avatars.push(newAvatar);
        totalAvatarsCount++;
        return totalAvatarsCount;
    }

    function GetAvatarsCount() public view returns (uint256 count) {
        return avatar.length;
    }

    function GetAvatarDetailsCount() public view returns (uint256 count) {
        return avatarDetails.length;
    }

    function GetHolonsCount() public view returns (uint256 count) {
        return holons.length;
    }
}