// SPDX-License-Identifier: MIT
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "../Enums/ProviderType.sol";
import "../Enums/KarmaSourceType.sol";
import "../Enums/KarmaTypePositive.sol";

struct Achievement {
    uint256 AchievementEarnt;
    KarmaTypePositive AchievementType;
    string AvatarId;
    string Description;
    KarmaSourceType KarmaSource;
    string KarmaSourceDesc;
    string KarmaSourceTitle;
    string Name;
    ProviderType Provider;
    string WebLink;
}
