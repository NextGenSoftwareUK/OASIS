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

    function SaveHolon(
        string parentOmiverseId,
        string parentMultiverseId,
        string parentUniverseId,
        string parentDimensionId,
        string parentGalaxyClusterId,
        string parentGalaxyId,
        string parentSolarSystemId,
        string parentGreatGrandSuperStarId,
        string parentGrandSuperStarId,
        string parentSuperStarId,
        string parentStarId,
        string parentPlanetId,
        string parentMoonId,
        string parentZomeId,
        string parentHolonId
    ) public returns (uint256) {
        Holon memory newHolon = Holon(
        {
            ParentOmiverseId: parentOmiverseId,
            ParentMultiverseId: parentMultiverseId,
            ParentUniverseId: parentUniverseId,
            ParentDimensionId: parentDimensionId,
            ParentGalaxyClusterId: parentGalaxyClusterId,
            ParentGalaxyId: parentGalaxyId,
            ParentSolarSystemId: parentSolarSystemId,
            ParentGreatGrandSuperStarId: parentGreatGrandSuperStarId,
            ParentGrandSuperStarId: parentGrandSuperStarId,
            ParentSuperStarId: parentSuperStarId,
            ParentStarId: parentStarId,
            ParentPlanetId: parentPlanetId,
            ParentMoonId: parentMoonId,
            ParentZomeId: parentZomeId,
            ParentHolonId: parentHolonId
        });
        holons.push(newHolon);
        totalHolonsCount++;
        return totalHolonsCount;
    }

    function SaveAvatarDetail(
        string avatarId,
        string title,
        string firstName,
        string lastName,
        string fullName,
        string username,
        string email,
        string avatarAddress,
        string country,
        string county,
        uint256 dob,
        string image2D,
        int karma,
        string landline,
        int level,
        string mobile,
        string model3D,
        string postcode,
        string town,
        string umaJson,
        int xp
    ) public returns (uint256) {
        AvatarDetail memory newHolon = AvatarDetail(
        {
            AvatarId: avatarId,
            Title: title,
            FirstName: firstName,
            LastName: lastName,
            FullName: fullName,
            Username: username,
            Email: email,
            Address: avatarAddress,
            Country: country,
            County: county,
            DOB: dob,
            Image2D: image2D,
            Karma: karma,
            Landline: landline,
            Level: level,
            Mobile: mobile,
            Model3D: model3D,
            Postcode: postcode,
            Town: town,
            UmaJson: umaJson,
            XP: xp
        });
        avatarDetails.push(newHolon);
        totalAvatarDetailsCount++;
        return totalAvatarDetailsCount;
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