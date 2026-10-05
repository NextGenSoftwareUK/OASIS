// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

struct RefreshToken {
    int Id;
    string AvatarId;
    string Token;
    uint256 Expires;
    bool IsExpired;
    uint256 Created;
    string CreatedByIp;
    uint256 Revoked;
    string RevokedByIp;
    string ReplacedByToken;
    bool IsActive;
}