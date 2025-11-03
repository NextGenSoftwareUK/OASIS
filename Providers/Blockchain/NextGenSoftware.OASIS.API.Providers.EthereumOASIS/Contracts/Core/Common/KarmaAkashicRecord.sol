// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "../Enums/ProviderType.sol";
import "../Enums/KarmaTypeNegative.sol";
import "../Enums/KarmaTypePositive.sol";
import "../Enums/KarmaSourceType.sol";
import "./KarmaEarntOrLost.sol";

struct KarmaAkashicRecord {
    string AvatarId;
    uint256 Date;
    int Karma;
    int TotalKarma;
    string KarmaSourceTitle;
    string KarmaSourceDesc;
    string WebLink;
    KarmaSourceType KarmaSource;
    KarmaEarntOrLost KarmaEarntOrLost;
    KarmaTypePositive KarmaTypePositive;
    KarmaTypeNegative KarmaTypeNegative;
    ProviderType Provider;
}
