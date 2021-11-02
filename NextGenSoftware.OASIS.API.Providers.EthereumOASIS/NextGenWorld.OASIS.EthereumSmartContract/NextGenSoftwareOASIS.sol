// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "./Core/Entities/AvatarDetail.sol";
import "./Core/Entities/Avatar.sol";
import "./Core/Entities/Holon.sol";
import "./Core/Enums/AvatarType.sol";

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

    function CreateAvatar(
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

    function CreateHolon(
        string holonId,
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
            HolonId: holonId,
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

    function CreateAvatarDetail(
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

    function UpdateAvatar(
        string memory avatarId,
        string memory title,
        string memory firstName,
        string memory lastName,
        string memory fullName,
        string memory username,
        string memory email,
        string memory password,
        bool memory acceptTerms,
        bool memory isVerified,
        string memory jwtToken,
        uint256 memory passwordReset,
        string memory refreshToken,
        string memory resetToken,
        uint256 memory resetTokenExpires,
        string memory verificationToken,
        uint256 memory verified,
        uint256 memory lastBeamedIn,
        uint256 memory lastBeamedOut,
        bool memory isBeamedIn,
        string memory image2D,
        int memory karma,
        int memory level,
        int memory xp
    ) public returns (bool) {
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].AvatarId == avatarId) {
                avatars[i].Title = title;
                avatars[i].FirstName = firstName;
                avatars[i].LastName = lastName;
                avatars[i].FullName = fullName;
                avatars[i].Username = username;
                avatars[i].Email = email;
                avatars[i].Password = password;
                avatars[i].AcceptTerms = acceptTerms;
                avatars[i].IsVerified = isVerified;
                avatars[i].JwtToken = jwtToken;
                avatars[i].PasswordReset = passwordReset;
                avatars[i].RefreshToken = refreshToken;
                avatars[i].ResetToken = resetToken;
                avatars[i].ResetTokenExpires = resetTokenExpires;
                avatars[i].VerificationToken = verificationToken;
                avatars[i].Verified = verified;
                avatars[i].LastBeamedIn = lastBeamedIn;
                avatars[i].LastBeamedIn = lastBeamedIn;
                avatars[i].IsBeamedIn = isBeamedIn;
                avatars[i].Image2D = image2D;
                avatars[i].Karma = karma;
                avatars[i].Level = level;
                avatars[i].XP = xp;
                return true;
            }
        }
        return false;
    }

    function UpdateAvatarDetail(
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
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].AvatarId == avatarId) {
                avatarDetails[i].Title = title;
                avatarDetails[i].FirstName = firstName;
                avatarDetails[i].LastName = lastName;
                avatarDetails[i].FullName = fullName;
                avatarDetails[i].Username = username;
                avatarDetails[i].Email = email;
                avatarDetails[i].Country = country;
                avatarDetails[i].County = county;
                avatarDetails[i].DOB = dob;
                avatarDetails[i].Address = avatarAddress;
                avatarDetails[i].Image2D = image2D;
                avatarDetails[i].Landline = landline;
                avatarDetails[i].Mobile = mobile;
                avatarDetails[i].Model3D = model3D;
                avatarDetails[i].Postcode = postcode;
                avatarDetails[i].Town = town;
                avatarDetails[i].UmaJson = umaJson;
                avatarDetails[i].Karma = karma;
                avatarDetails[i].Level = level;
                avatarDetails[i].XP = xp;
                return true;
            }
        }
        return false;
    }

    function UpdateHolon(
        string holonId,
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
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (holons[i].HolonId == holonId) {
                holons[i].ParentOmiverseId = parentOmiverseId;
                holons[i].parentOmiverseId = parentMultiverseId;
                holons[i].parentOmiverseId = parentUniverseId;
                holons[i].parentOmiverseId = parentDimensionId;
                holons[i].parentOmiverseId = parentGalaxyClusterId;
                holons[i].parentOmiverseId = parentSolarSystemId;
                holons[i].parentOmiverseId = parentGreatGrandSuperStarId;
                holons[i].parentOmiverseId = parentGalaxyId;
                holons[i].parentOmiverseId = parentSuperStarId;
                holons[i].parentOmiverseId = parentStarId;
                holons[i].parentOmiverseId = parentPlanetId;
                holons[i].parentOmiverseId = parentMoonId;
                holons[i].parentOmiverseId = ParentOmiverseId;
                holons[i].parentOmiverseId = parentZomeId;
                holons[i].parentOmiverseId = parentHolonId;
                return true;
            }
        }
        return false;
    }

    function DeleteAvatar(string memory avatarId) public returns (bool) {
        require(totalAvatarsCount > 0);
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].AvatarId == avatarId) {
                avatars[i] = avatars[totalAvatarsCount - 1];
                delete avatars[totalAvatarsCount - 1];
                totalAvatarsCount--;
                return true;
            }
        }
        return false;
    }
    
    function DeleteAvatarDetail(string memory avatarDetailId) public returns (bool) {
        require(totalAvatarDetailsCount > 0);
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].AvatarId == avatarDetailId) {
                avatarDetails[i] = avatarDetails[totalAvatarDetailsCount - 1];
                delete avatarDetails[totalAvatarDetailsCount - 1];
                totalAvatarDetailsCount--;
                return true;
            }
        }
        return false;
    }
    
    function DeleteHolon(string memory holonId) public returns (bool) {
        require(totalHolonsCount > 0);
        for (uint256 i = 0; i < totalHolonsCount; i++) {
            if (holons[i].HolonId == holonId) {
                holons[i] = holons[totalHolonsCount - 1];
                delete holons[totalHolonsCount - 1];
                totalHolonsCount--;
                return true;
            }
        }
        return false;
    }
    
    function GetAvatarById(string memory avatarId) public view returns (Avatar memory)
    {
        require(totalAvatarsCount > 0);
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].AvatarId == avatarId) {
                return avatars[i];
            }
        }
        return;
    }

    function GetAvatarDetailById(string memory avatarId) public view returns (AvatarDetail memory)
    {
        require(totalAvatarDetailsCount > 0);
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].AvatarId == avatarId) {
                return avatarDetails[i];
            }
        }
        return;
    }

    function GetHolonById(string memory holonId) public view returns (Holon memory)
    {
        require(totalHolonsCount > 0);
        for (uint256 i = 0; i < totalHolonsCount; i++) {
            if (holons[i].HolonId == holonId) {
                return holons[i];
            }
        }
        return;
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