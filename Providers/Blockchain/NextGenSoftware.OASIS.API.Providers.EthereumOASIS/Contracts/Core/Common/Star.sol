// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "../Enums/StarType.sol";
import "../Enums/StarClassification.sol";
import "../Enums/StarBinaryType.sol";

struct Star {
    int Luminosity;
    StarType StarType;
    StarClassification StarClassification;
    StarBinaryType StarBinaryType;
}
