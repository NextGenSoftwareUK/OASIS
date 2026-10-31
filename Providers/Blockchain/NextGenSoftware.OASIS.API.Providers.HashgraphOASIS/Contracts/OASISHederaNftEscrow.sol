// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

/// @dev HTS NFT redirect exposes ERC721 and HIP-719 association methods.
interface IHederaEscrowNFT {
    function ownerOf(uint256 serial) external view returns (address);
    function transferFrom(address from, address to, uint256 serial) external;
    function isAssociated() external view returns (bool);
    function associate() external returns (uint256);
}

/// @notice Depositor-owned NFT custody; no administrator or arbitrary release recipient.
contract OASISHederaNftEscrow {
    mapping(address => mapping(uint256 => address)) public nftDepositor;
    bool private entered;
    event NFTLocked(address indexed token, uint256 indexed serial, address indexed owner);
    event NFTUnlocked(address indexed token, uint256 indexed serial, address indexed owner);

    modifier nonReentrant() {
        require(!entered, "Reentrant escrow call");
        entered = true;
        _;
        entered = false;
    }

    function lockNFT(address tokenAddress, uint256 serial) external nonReentrant {
        require(tokenAddress != address(0) && tokenAddress != address(this), "Invalid token");
        require(serial > 0 && serial <= uint256(uint64(type(int64).max)), "Invalid HTS serial");
        require(nftDepositor[tokenAddress][serial] == address(0), "Already deposited");
        IHederaEscrowNFT token = IHederaEscrowNFT(tokenAddress);
        require(token.ownerOf(serial) == msg.sender, "Not NFT owner");
        if (!token.isAssociated()) require(token.associate() == 22, "HTS association failed");
        nftDepositor[tokenAddress][serial] = msg.sender;
        token.transferFrom(msg.sender, address(this), serial);
        require(token.ownerOf(serial) == address(this), "NFT deposit failed");
        emit NFTLocked(tokenAddress, serial, msg.sender);
    }

    function unlockNFT(address tokenAddress, uint256 serial) external nonReentrant {
        require(nftDepositor[tokenAddress][serial] == msg.sender, "No owned NFT deposit");
        IHederaEscrowNFT token = IHederaEscrowNFT(tokenAddress);
        require(token.ownerOf(serial) == address(this), "NFT custody lost");
        delete nftDepositor[tokenAddress][serial];
        token.transferFrom(address(this), msg.sender, serial);
        require(token.ownerOf(serial) == msg.sender, "NFT withdrawal failed");
        emit NFTUnlocked(tokenAddress, serial, msg.sender);
    }
}
