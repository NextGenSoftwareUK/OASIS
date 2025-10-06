// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "../Enums/ChakraType.sol";
import "../Enums/ElementType.sol";
import "../Enums/YogaPoseType.sol";
import "./AvatarGift.sol";
import "./Crystal.sol";

struct Chakra {
    string Name;
    ChakraType Type;
    string SanskritName;
    string Description;
    ElementType Element;
    Crystal Crystal;
    YogaPoseType YogaPose;
    string WhatItControls;
    string WhenItDevelops;
    int Level;
    int Progress;
    int XP;
    AvatarGift[] GiftsUnlocked;
}
