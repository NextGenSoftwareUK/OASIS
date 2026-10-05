// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "./MultiverseDimensions.sol";
import "./GrandSuperStar.sol";

struct Multiverse {
    GrandSuperStar GrandSuperStar;
    MultiverseDimensions MultiverseDimensions;
}