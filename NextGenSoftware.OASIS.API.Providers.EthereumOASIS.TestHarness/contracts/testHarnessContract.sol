// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

contract testHarnessContract {
    int public _multiplier;

    constructor(int multiplier) public {
        _multiplier = multiplier;
    }

    function multiply(int val) public view returns (int result) {
        return val * _multiplier;
    }
}