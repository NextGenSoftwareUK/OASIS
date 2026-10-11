// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface ITRC20EscrowToken {
    function balanceOf(address account) external view returns (uint256);
    function transferFrom(address from, address to, uint256 amount) external returns (bool);
    function transfer(address to, uint256 amount) external returns (bool);
}

/// @notice Reversible, depositor-owned escrow. No administrator can withdraw user deposits.
/// @dev The current lock request has no amount: lockAll deposits the caller's spendable balance.
///      The caller must approve this contract first. Fee-on-transfer tokens are rejected.
contract OASISTokenEscrow {
    mapping(address => mapping(address => uint256)) public locked;
    bool private entered;

    event Locked(address indexed token, address indexed owner, uint256 amount);
    event Unlocked(address indexed token, address indexed owner, uint256 amount);

    modifier nonReentrant() {
        require(!entered, "Reentrant escrow call");
        entered = true;
        _;
        entered = false;
    }

    function lockAll(address tokenAddress) external nonReentrant returns (uint256 amount) {
        require(tokenAddress != address(0) && tokenAddress != address(this), "Invalid token");
        ITRC20EscrowToken token = ITRC20EscrowToken(tokenAddress);
        amount = token.balanceOf(msg.sender);
        require(amount > 0, "No spendable tokens");
        uint256 beforeBalance = token.balanceOf(address(this));
        require(token.transferFrom(msg.sender, address(this), amount), "Token deposit failed");
        require(token.balanceOf(address(this)) == beforeBalance + amount, "Inexact token deposit");
        locked[tokenAddress][msg.sender] += amount;
        emit Locked(tokenAddress, msg.sender, amount);
    }

    function unlock(address tokenAddress) external nonReentrant returns (uint256 amount) {
        amount = locked[tokenAddress][msg.sender];
        require(amount > 0, "No owned deposit");
        locked[tokenAddress][msg.sender] = 0;
        ITRC20EscrowToken token = ITRC20EscrowToken(tokenAddress);
        uint256 beforeBalance = token.balanceOf(msg.sender);
        require(token.transfer(msg.sender, amount), "Token withdrawal failed");
        require(token.balanceOf(msg.sender) == beforeBalance + amount, "Inexact token withdrawal");
        emit Unlocked(tokenAddress, msg.sender, amount);
    }
}
