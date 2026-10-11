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
interface ITRC721EscrowToken {
    function ownerOf(uint256 tokenId) external view returns (address);
    function transferFrom(address from, address to, uint256 tokenId) external;
}

contract OASISTokenEscrow {
    mapping(address => mapping(address => uint256)) public locked;
    mapping(address => mapping(uint256 => address)) public nftDepositor;
    bool private entered;

    event Locked(address indexed token, address indexed owner, uint256 amount);
    event Unlocked(address indexed token, address indexed owner, uint256 amount);
    event NFTLocked(address indexed token, uint256 indexed tokenId, address indexed owner);
    event NFTUnlocked(address indexed token, uint256 indexed tokenId, address indexed owner);

    modifier nonReentrant() {
        require(!entered, "Reentrant escrow call");
        entered = true;
        _;
        entered = false;
    }

    function lockNFT(address tokenAddress, uint256 tokenId) external nonReentrant {
        require(tokenAddress != address(0) && tokenAddress != address(this), "Invalid token");
        require(nftDepositor[tokenAddress][tokenId] == address(0), "Already deposited");
        ITRC721EscrowToken token = ITRC721EscrowToken(tokenAddress);
        require(token.ownerOf(tokenId) == msg.sender, "Not NFT owner");
        nftDepositor[tokenAddress][tokenId] = msg.sender;
        token.transferFrom(msg.sender, address(this), tokenId);
        require(token.ownerOf(tokenId) == address(this), "NFT deposit failed");
        emit NFTLocked(tokenAddress, tokenId, msg.sender);
    }

    function unlockNFT(address tokenAddress, uint256 tokenId) external nonReentrant {
        require(nftDepositor[tokenAddress][tokenId] == msg.sender, "No owned NFT deposit");
        delete nftDepositor[tokenAddress][tokenId];
        ITRC721EscrowToken token = ITRC721EscrowToken(tokenAddress);
        require(token.ownerOf(tokenId) == address(this), "NFT custody lost");
        token.transferFrom(address(this), msg.sender, tokenId);
        require(token.ownerOf(tokenId) == msg.sender, "NFT withdrawal failed");
        emit NFTUnlocked(tokenAddress, tokenId, msg.sender);
    }

    function lockAll(address tokenAddress) external nonReentrant returns (uint256 amount) {
        require(tokenAddress != address(0) && tokenAddress != address(this), "Invalid token");
        ITRC20EscrowToken token = ITRC20EscrowToken(tokenAddress);
        amount = token.balanceOf(msg.sender);
        require(amount > 0, "No spendable tokens");
        uint256 beforeBalance = token.balanceOf(address(this));
        require(token.transferFrom(msg.sender, address(this), amount), "Token deposit failed");
        require(token.balanceOf(address(this)) == beforeBalance + amount, "Inexact token deposit");
        require(token.balanceOf(msg.sender) == 0, "Inexact depositor debit");
        locked[tokenAddress][msg.sender] += amount;
        emit Locked(tokenAddress, msg.sender, amount);
    }

    function unlock(address tokenAddress) external nonReentrant returns (uint256 amount) {
        amount = locked[tokenAddress][msg.sender];
        require(amount > 0, "No owned deposit");
        locked[tokenAddress][msg.sender] = 0;
        ITRC20EscrowToken token = ITRC20EscrowToken(tokenAddress);
        uint256 beforeBalance = token.balanceOf(msg.sender);
        uint256 beforeEscrow = token.balanceOf(address(this));
        require(token.transfer(msg.sender, amount), "Token withdrawal failed");
        require(token.balanceOf(msg.sender) == beforeBalance + amount, "Inexact token withdrawal");
        require(beforeEscrow >= amount && token.balanceOf(address(this)) == beforeEscrow - amount, "Inexact escrow debit");
        emit Unlocked(tokenAddress, msg.sender, amount);
    }
}
