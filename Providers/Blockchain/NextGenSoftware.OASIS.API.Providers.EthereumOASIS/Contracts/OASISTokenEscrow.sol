// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

interface IERC20DepositToken {
    function balanceOf(address owner) external view returns (uint256);
    function totalSupply() external view returns (uint256);
    function transferFrom(address from, address to, uint256 amount) external returns (bool);
    function transfer(address to, uint256 amount) external returns (bool);
}

/// @notice Explicit, single-use deposits owned exclusively by their depositor.
/// @dev Conventional exact-transfer ERC20 only. No admin withdrawals or lock-all default.
contract OASISTokenEscrow {
    uint256 public constant escrowProtocolVersion = 1;
    struct Deposit { address owner; address token; uint256 amount; bool released; }
    mapping(bytes32 => Deposit) public deposits;
    bool private entered;

    event DepositLocked(bytes32 indexed id, address indexed owner, address indexed token, uint256 amount);
    event DepositReleased(bytes32 indexed id, address indexed owner, address indexed token, uint256 amount);

    modifier nonReentrant() {
        require(!entered, "Reentrant escrow call");
        entered = true;
        _;
        entered = false;
    }

    function lock(bytes32 id, address tokenAddress, uint256 amount) external nonReentrant {
        require(id != bytes32(0), "Empty deposit id");
        require(deposits[id].owner == address(0), "Deposit id already used");
        require(tokenAddress != address(this) && tokenAddress.code.length > 0, "Invalid token");
        require(amount > 0, "Empty deposit amount");
        IERC20DepositToken token = IERC20DepositToken(tokenAddress);
        uint256 ownerBefore = token.balanceOf(msg.sender);
        uint256 escrowBefore = token.balanceOf(address(this));
        uint256 supplyBefore = token.totalSupply();
        require(ownerBefore >= amount, "Insufficient token balance");
        deposits[id] = Deposit(msg.sender, tokenAddress, amount, false);
        require(token.transferFrom(msg.sender, address(this), amount), "Token deposit failed");
        require(token.balanceOf(msg.sender) == ownerBefore - amount, "Inexact depositor debit");
        require(token.balanceOf(address(this)) == escrowBefore + amount, "Inexact escrow credit");
        require(token.totalSupply() == supplyBefore, "Token supply changed");
        emit DepositLocked(id, msg.sender, tokenAddress, amount);
    }

    function unlock(bytes32 id) external nonReentrant {
        Deposit storage deposit = deposits[id];
        require(deposit.owner == msg.sender, "Not deposit owner");
        require(!deposit.released, "Deposit already released");
        IERC20DepositToken token = IERC20DepositToken(deposit.token);
        uint256 ownerBefore = token.balanceOf(msg.sender);
        uint256 escrowBefore = token.balanceOf(address(this));
        uint256 supplyBefore = token.totalSupply();
        require(escrowBefore >= deposit.amount, "Escrow custody lost");
        deposit.released = true;
        require(token.transfer(msg.sender, deposit.amount), "Token release failed");
        require(token.balanceOf(msg.sender) == ownerBefore + deposit.amount, "Inexact depositor credit");
        require(token.balanceOf(address(this)) == escrowBefore - deposit.amount, "Inexact escrow debit");
        require(token.totalSupply() == supplyBefore, "Token supply changed");
        emit DepositReleased(id, msg.sender, deposit.token, deposit.amount);
    }
}
