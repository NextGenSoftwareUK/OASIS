// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "../Enums/KarmaTypePositive.sol";
import "../Enums/KarmaSourceType.sol";
import "../Enums/ProviderType.sol";

struct AvatarGift {
    string AvatarId;
    uint256 GiftEarnt;
    KarmaTypePositive GiftType;
    KarmaSourceType KarmaSource;
    string KarmaSourceDesc;
    string KarmaSourceTitle;
    ProviderType Provider;
    string WebLink;
}
