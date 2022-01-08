// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "../Enums/AvatarType.sol";
import "../Common/RefreshToken.sol";

struct Avatar {
    string AvatarId;
    string Title;
    string FirstName;
    string LastName;
    string FullName;
    string Username;
    string Email;
    string Password;
    // AvatarType AvatarType;
    bool AcceptTerms;
    bool IsVerified;
    string JwtToken;
    uint256 PasswordReset;
    string RefreshToken;
    // RefreshToken[] RefreshTokens;
    string ResetToken;
    uint256 ResetTokenExpires;
    string VerificationToken;
    uint256 Verified;
    uint256 LastBeamedIn;
    uint256 LastBeamedOut;
    bool IsBeamedIn;
    string Image2D;
    int Karma;
    int Level;
    int XP;
}