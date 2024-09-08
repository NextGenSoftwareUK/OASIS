// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.20;

struct NFTTransfer {
    address fromWalletAddress;
    address toWalletAddress;
    string fromProviderType;
    string toProviderType;
    uint256 amount;
    string memoText;
}