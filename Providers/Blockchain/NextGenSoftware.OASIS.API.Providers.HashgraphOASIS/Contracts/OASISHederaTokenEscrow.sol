// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;
import "./OASISHederaNftEscrow.sol";

interface IHederaEscrowToken {
    function balanceOf(address owner) external view returns (uint256);
    function transferFrom(address from, address to, uint256 amount) external returns (bool);
    function transfer(address to, uint256 amount) external returns (bool);
    function isAssociated() external view returns (bool);
    function associate() external returns (uint256);
}

/// @notice HTS fungible and NFT custody owned exclusively by each depositor.
/// @dev Existing fungible lock request has no amount: deposits all spendable units.
contract OASISHederaTokenEscrow is OASISHederaNftEscrow {
    mapping(address => mapping(address => uint256)) public locked;
    event Locked(address indexed token, address indexed owner, uint256 amount);
    event Unlocked(address indexed token, address indexed owner, uint256 amount);

    function lockAll(address tokenAddress) external nonReentrant returns (uint256 amount) {
        require(tokenAddress != address(0) && tokenAddress != address(this), "Invalid token");
        IHederaEscrowToken token = IHederaEscrowToken(tokenAddress);
        amount = token.balanceOf(msg.sender);
        require(amount > 0 && amount <= uint256(uint64(type(int64).max)), "Invalid HTS amount");
        if (!token.isAssociated()) require(token.associate() == 22, "HTS association failed");
        uint256 beforeEscrow = token.balanceOf(address(this));
        require(token.transferFrom(msg.sender, address(this), amount), "Token deposit failed");
        require(token.balanceOf(address(this)) == beforeEscrow + amount, "Inexact escrow credit");
        require(token.balanceOf(msg.sender) == 0, "Inexact depositor debit");
        locked[tokenAddress][msg.sender] += amount;
        emit Locked(tokenAddress, msg.sender, amount);
    }

    function unlock(address tokenAddress) external nonReentrant returns (uint256 amount) {
        amount = locked[tokenAddress][msg.sender];
        require(amount > 0, "No owned token deposit");
        IHederaEscrowToken token = IHederaEscrowToken(tokenAddress);
        uint256 beforeOwner = token.balanceOf(msg.sender);
        uint256 beforeEscrow = token.balanceOf(address(this));
        locked[tokenAddress][msg.sender] = 0;
        require(token.transfer(msg.sender, amount), "Token withdrawal failed");
        require(token.balanceOf(msg.sender) == beforeOwner + amount, "Inexact owner credit");
        require(beforeEscrow >= amount && token.balanceOf(address(this)) == beforeEscrow - amount, "Inexact escrow debit");
        emit Unlocked(tokenAddress, msg.sender, amount);
    }
}
