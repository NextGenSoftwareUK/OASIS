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
    
    constructor() {
        totalAvatarsCount = 0;
        totalAvatarDetailsCount = 0;
        totalHolonsCount = 0;
    }

    function CreateAvatar(
        uint256 entityId,
        string memory avatarId,
        string memory info
        // string memory title,
        // string memory firstName,
        // string memory lastName,
        // string memory fullName,
        // string memory username,
        // string memory email,
        // string memory password,
        // bool acceptTerms,
        // bool isVerified,
        // string memory jwtToken,
        // uint256 passwordReset,
        // string memory refreshToken,
        // string memory resetToken,
        // uint256 resetTokenExpires,
        // string memory verificationToken,
        // uint256 verified,
        // uint256 lastBeamedIn,
        // uint256 lastBeamedOut,
        // bool isBeamedIn,
        // string memory image2D,
        // int karma,
        // int level,
        // int xp
    ) public returns (uint256) {
        Avatar memory newAvatar = Avatar({
            AvatarId: avatarId,
            EntityId: entityId,
            Info: info
            // Title: title,
            // FirstName: firstName,
            // LastName: lastName,
            // FullName: fullName,
            // Username: username,
            // Email: email,
            // Password: password,
            // AcceptTerms: acceptTerms,
            // IsVerified: isVerified,
            // JwtToken: jwtToken,
            // PasswordReset: passwordReset,
            // RefreshToken: refreshToken,
            // ResetToken: resetToken,
            // ResetTokenExpires: resetTokenExpires,
            // VerificationToken: verificationToken,
            // Verified: verified,
            // LastBeamedIn: lastBeamedIn,
            // LastBeamedOut: lastBeamedOut,
            // IsBeamedIn: isBeamedIn,
            // Image2D: image2D,
            // Karma: karma,
            // Level: level,
            // XP: xp
        });
        avatars.push(newAvatar);
        totalAvatarsCount++;
        return totalAvatarsCount;
    }

    function CreateHolon(
        uint256 entityId,
        string memory holonId,
        string memory info
        // string memory parentOmiverseId,
        // string memory parentMultiverseId,
        // string memory parentUniverseId,
        // string memory parentDimensionId,
        // string memory parentGalaxyClusterId,
        // string memory parentGalaxyId,
        // string memory parentSolarSystemId,
        // string memory parentGreatGrandSuperStarId,
        // string memory parentGrandSuperStarId,
        // string memory parentSuperStarId,
        // string memory parentStarId,
        // string memory parentPlanetId,
        // string memory parentMoonId,
        // string memory parentZomeId,
        // string memory parentHolonId
    ) public returns (uint256) {
        Holon memory newHolon = Holon(
        {
            EntityId: entityId,
            HolonId: holonId,
            Info: info
            // ParentOmiverseId: parentOmiverseId,
            // ParentMultiverseId: parentMultiverseId,
            // ParentUniverseId: parentUniverseId,
            // ParentDimensionId: parentDimensionId,
            // ParentGalaxyClusterId: parentGalaxyClusterId,
            // ParentGalaxyId: parentGalaxyId,
            // ParentSolarSystemId: parentSolarSystemId,
            // ParentGreatGrandSuperStarId: parentGreatGrandSuperStarId,
            // ParentGrandSuperStarId: parentGrandSuperStarId,
            // ParentSuperStarId: parentSuperStarId,
            // ParentStarId: parentStarId,
            // ParentPlanetId: parentPlanetId,
            // ParentMoonId: parentMoonId,
            // ParentZomeId: parentZomeId,
            // ParentHolonId: parentHolonId
        });
        holons.push(newHolon);
        totalHolonsCount++;
        return totalHolonsCount;
    }

    function CreateAvatarDetail(
        string memory avatarId,
        string memory info,
        uint256 entityId
        // string memory title,
        // string memory firstName,
        // string memory lastName,
        // string memory fullName,
        // string memory username,
        // string memory email,
        // string memory avatarAddress,
        // string memory country,
        // string memory county,
        // uint256 dob,
        // string memory image2D,
        // int karma,
        // string memory landline,
        // int level,
        // string memory mobile,
        // string memory model3D,
        // string memory postcode,
        // string memory town,
        // string memory umaJson,
        // int xp
    ) public returns (uint256) {
        AvatarDetail memory newAvatarDetail = AvatarDetail(
        {
            AvatarId: avatarId,
            EntityId: entityId,
            Info: info
            // avatarId,
            // title,
            // firstName,
            // lastName,
            // fullName,
            // username,
            // email,
            // avatarAddress,
            // country,
            // county,
            // dob,
            // image2D,
            // karma,
            // landline,
            // level,
            // mobile,
            // model3D,
            // postcode,
            // town,
            // umaJson,
            // xp
        });
        avatarDetails.push(newAvatarDetail);
        totalAvatarDetailsCount++;
        return totalAvatarDetailsCount;
    }

    // function UpdateAvatar(
    //     string memory avatarId,
    //     string memory title,
    //     string memory firstName,
    //     string memory lastName,
    //     string memory fullName,
    //     string memory username,
    //     string memory email,
    //     string memory password,
    //     bool acceptTerms,
    //     bool isVerified,
    //     string memory jwtToken,
    //     uint256 passwordReset,
    //     string memory refreshToken,
    //     string memory resetToken,
    //     uint256 resetTokenExpires,
    //     string memory verificationToken,
    //     uint256 verified,
    //     uint256 lastBeamedIn,
    //     uint256 lastBeamedOut,
    //     bool isBeamedIn,
    //     string memory image2D,
    //     int karma,
    //     int level,
    //     int xp
    // ) public returns (bool) {
    //     for (uint256 i = 0; i < totalAvatarsCount; i++) {
    //         if (keccak256(abi.encodePacked(avatars[i].AvatarId)) == keccak256(abi.encodePacked(avatarId))) {
    //             avatars[i].Title = title;
    //             avatars[i].FirstName = firstName;
    //             avatars[i].LastName = lastName;
    //             avatars[i].FullName = fullName;
    //             avatars[i].Username = username;
    //             avatars[i].Email = email;
    //             avatars[i].Password = password;
    //             avatars[i].AcceptTerms = acceptTerms;
    //             avatars[i].IsVerified = isVerified;
    //             avatars[i].JwtToken = jwtToken;
    //             avatars[i].PasswordReset = passwordReset;
    //             avatars[i].RefreshToken = refreshToken;
    //             avatars[i].ResetToken = resetToken;
    //             avatars[i].ResetTokenExpires = resetTokenExpires;
    //             avatars[i].VerificationToken = verificationToken;
    //             avatars[i].Verified = verified;
    //             avatars[i].LastBeamedIn = lastBeamedIn;
    //             avatars[i].LastBeamedOut = lastBeamedOut;
    //             avatars[i].IsBeamedIn = isBeamedIn;
    //             avatars[i].Image2D = image2D;
    //             avatars[i].Karma = karma;
    //             avatars[i].Level = level;
    //             avatars[i].XP = xp;
    //             return true;
    //         }
    //     }
    //     return false;
    // }

    // function UpdateAvatarDetail(
    //     string memory avatarDetailId,
    //     string memory title,
    //     string memory firstName,
    //     string memory lastName,
    //     string memory fullName,
    //     string memory username,
    //     string memory email,
    //     string memory avatarAddress,
    //     string memory country,
    //     string memory county,
    //     uint256 dob,
    //     string memory image2D,
    //     int karma,
    //     string memory landline,
    //     int level,
    //     string memory mobile,
    //     string memory model3D,
    //     string memory postcode,
    //     string memory town,
    //     string memory umaJson,
    //     int xp
    // ) public returns (bool) {
    //     for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
    //         if (keccak256(abi.encodePacked(avatars[i].AvatarId)) == keccak256(abi.encodePacked(avatarDetailId))) {
    //             avatarDetails[i].Title = title;
    //             avatarDetails[i].FirstName = firstName;
    //             avatarDetails[i].LastName = lastName;
    //             avatarDetails[i].FullName = fullName;
    //             avatarDetails[i].Username = username;
    //             avatarDetails[i].Email = email;
    //             avatarDetails[i].Country = country;
    //             avatarDetails[i].County = county;
    //             avatarDetails[i].DOB = dob;
    //             avatarDetails[i].Address = avatarAddress;
    //             avatarDetails[i].Image2D = image2D;
    //             avatarDetails[i].Landline = landline;
    //             avatarDetails[i].Mobile = mobile;
    //             avatarDetails[i].Model3D = model3D;
    //             avatarDetails[i].Postcode = postcode;
    //             avatarDetails[i].Town = town;
    //             avatarDetails[i].UmaJson = umaJson;
    //             avatarDetails[i].Karma = karma;
    //             avatarDetails[i].Level = level;
    //             avatarDetails[i].XP = xp;
    //             return true;
    //         }
    //     }
    //     return false;
    // }

    // function UpdateHolon(
    //     string memory holonId,
    //     string memory parentOmiverseId,
    //     string memory parentMultiverseId,
    //     string memory parentUniverseId,
    //     string memory parentDimensionId,
    //     string memory parentGalaxyClusterId,
    //     string memory parentGalaxyId,
    //     string memory parentSolarSystemId,
    //     string memory parentGreatGrandSuperStarId,
    //     string memory parentGrandSuperStarId,
    //     string memory parentSuperStarId,
    //     string memory parentStarId,
    //     string memory parentPlanetId,
    //     string memory parentMoonId,
    //     string memory parentZomeId,
    //     string memory parentHolonId
    // ) public returns (bool) {
    //     for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
    //         if (keccak256(abi.encodePacked(holons[i].HolonId)) == keccak256(abi.encodePacked(holonId))) {
    //             holons[i].ParentOmiverseId = parentOmiverseId;
    //             holons[i].ParentMultiverseId = parentMultiverseId;
    //             holons[i].ParentUniverseId = parentUniverseId;
    //             holons[i].ParentDimensionId = parentDimensionId;
    //             holons[i].ParentGalaxyClusterId = parentGalaxyClusterId;
    //             holons[i].ParentSolarSystemId = parentSolarSystemId;
    //             holons[i].ParentGreatGrandSuperStarId = parentGreatGrandSuperStarId;
    //             holons[i].ParentGalaxyId = parentGalaxyId;
    //             holons[i].ParentGrandSuperStarId = parentGrandSuperStarId;
    //             holons[i].ParentSuperStarId = parentSuperStarId;
    //             holons[i].ParentStarId = parentStarId;
    //             holons[i].ParentPlanetId = parentPlanetId;
    //             holons[i].ParentMoonId = parentMoonId;
    //             holons[i].ParentOmiverseId = parentOmiverseId;
    //             holons[i].ParentZomeId = parentZomeId;
    //             holons[i].ParentHolonId = parentHolonId;
    //             return true;
    //         }
    //     }
    //     return false;
    // }

    // function DeleteAvatar(string memory avatarId) public returns (bool) {
    //     require(totalAvatarsCount > 0);
    //     for (uint256 i = 0; i < totalAvatarsCount; i++) {
    //         if (keccak256(abi.encodePacked(avatars[i].AvatarId)) == keccak256(abi.encodePacked(avatarId))) {
    //             avatars[i] = avatars[totalAvatarsCount - 1];
    //             delete avatars[totalAvatarsCount - 1];
    //             totalAvatarsCount--;
    //             return true;
    //         }
    //     }
    //     return false;
    // }
    
    // function DeleteAvatarDetail(string memory avatarDetailId) public returns (bool) {
    //     require(totalAvatarDetailsCount > 0);
    //     for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
    //         if (keccak256(abi.encodePacked(avatarDetails[i].AvatarId)) == keccak256(abi.encodePacked(avatarDetailId))) {
    //             avatarDetails[i] = avatarDetails[totalAvatarDetailsCount - 1];
    //             delete avatarDetails[totalAvatarDetailsCount - 1];
    //             totalAvatarDetailsCount--;
    //             return true;
    //         }
    //     }
    //     return false;
    // }
    
    // function DeleteHolon(string memory holonId) public returns (bool) {
    //     require(totalHolonsCount > 0);
    //     for (uint256 i = 0; i < totalHolonsCount; i++) {
    //         if (keccak256(abi.encodePacked(holons[i].HolonId)) == keccak256(abi.encodePacked(holonId))) {
    //             holons[i] = holons[totalHolonsCount - 1];
    //             delete holons[totalHolonsCount - 1];
    //             totalHolonsCount--;
    //             return true;
    //         }
    //     }
    //     return false;
    // }
    
    // function GetAvatarById(string memory avatarId) public view returns (Avatar memory)
    // {
    //     Avatar memory avatar;
    //     require(totalAvatarsCount > 0);
    //     for (uint256 i = 0; i < totalAvatarsCount; i++) {
    //         if (keccak256(abi.encodePacked(avatars[i].AvatarId)) == keccak256(abi.encodePacked(avatarId))) {
    //             return avatars[i];
    //         }
    //     }
    //     return avatar;
    // }

    // function GetAvatarDetailById(string memory avatarId) public view returns (AvatarDetail memory)
    // {
    //     AvatarDetail memory avatarDetail;
    //     require(totalAvatarDetailsCount > 0);
    //     for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
    //         if (keccak256(abi.encodePacked(avatarDetails[i].AvatarId)) == keccak256(abi.encodePacked(avatarId))) {
    //             return avatarDetails[i];
    //         }
    //     }
    //     return avatarDetail;
    // }

    // function GetHolonById(string memory holonId) public view returns (Holon memory)
    // {
    //     Holon memory holon;
    //     require(totalHolonsCount > 0);
    //     for (uint256 i = 0; i < totalHolonsCount; i++) {
    //         if (keccak256(abi.encodePacked(holons[i].HolonId)) == keccak256(abi.encodePacked(holonId))) {
    //             return holons[i];
    //         }
    //     }
    //     return holon;
    // }
    
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