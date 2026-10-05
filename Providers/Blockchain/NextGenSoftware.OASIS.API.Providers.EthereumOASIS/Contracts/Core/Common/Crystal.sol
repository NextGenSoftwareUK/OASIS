// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "../Enums/CrystalName.sol";
import "../Enums/CrystalType.sol";

struct Crystal {
    CrystalName Name;
    string Description;
    CrystalType Type;
    int AmplifyicationLevel;
    int CleansingLevel;
    int EnergisingLevel;
    int GroundingLevel;
    int ProtectionLevel;
}
