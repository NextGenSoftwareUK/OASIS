// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

/**
 * @title OASIS Smart Contract
 * @dev Comprehensive smart contract for OASIS blockchain operations
 * @author NextGen Software Ltd
 * @notice This contract handles Avatar, Holon, and NFT operations on EVM-compatible chains
 */
contract OASIS {
    // Events
    event AvatarCreated(address indexed owner, string avatarId, string username);
    event AvatarUpdated(address indexed owner, string avatarId, string username);
    event AvatarDeleted(address indexed owner, string avatarId);
    
    event HolonCreated(address indexed owner, string holonId, string name);
    event HolonUpdated(address indexed owner, string holonId, string name);
    event HolonDeleted(address indexed owner, string holonId);
    
    event NFTMinted(address indexed owner, uint256 tokenId, string tokenURI);
    event NFTSent(address indexed from, address indexed to, uint256 tokenId);
    
    // Structs
    struct Avatar {
        string id;
        string username;
        string email;
        string firstName;
        string lastName;
        string avatarType;
        string metadata;
        uint256 createdDate;
        uint256 modifiedDate;
        bool exists;
    }
    
    struct Holon {
        string id;
        string name;
        string description;
        string holonType;
        string metadata;
        string parentId;
        uint256 createdDate;
        uint256 modifiedDate;
        bool exists;
    }
    
    struct NFT {
        uint256 tokenId;
        string tokenURI;
        address owner;
        string metadata;
        uint256 createdDate;
        bool exists;
    }
    
    // State variables
    mapping(string => Avatar) public avatars;
    mapping(string => Holon) public holons;
    mapping(uint256 => NFT) public nfts;
    mapping(address => string[]) public userAvatars;
    mapping(address => string[]) public userHolons;
    mapping(address => uint256[]) public userNFTs;
    
    uint256 public nextTokenId = 1;
    address public owner;
    
    // Modifiers
    modifier onlyOwner() {
        require(msg.sender == owner, "Only owner can call this function");
        _;
    }
    
    modifier avatarExists(string memory avatarId) {
        require(avatars[avatarId].exists, "Avatar does not exist");
        _;
    }
    
    modifier holonExists(string memory holonId) {
        require(holons[holonId].exists, "Holon does not exist");
        _;
    }
    
    modifier nftExists(uint256 tokenId) {
        require(nfts[tokenId].exists, "NFT does not exist");
        _;
    }
    
    // Constructor
    constructor() {
        owner = msg.sender;
    }
    
    // Avatar Functions
    function createAvatar(
        string memory avatarId,
        string memory username,
        string memory email,
        string memory firstName,
        string memory lastName,
        string memory avatarType,
        string memory metadata
    ) external returns (bool) {
        require(!avatars[avatarId].exists, "Avatar already exists");
        
        avatars[avatarId] = Avatar({
            id: avatarId,
            username: username,
            email: email,
            firstName: firstName,
            lastName: lastName,
            avatarType: avatarType,
            metadata: metadata,
            createdDate: block.timestamp,
            modifiedDate: block.timestamp,
            exists: true
        });
        
        userAvatars[msg.sender].push(avatarId);
        
        emit AvatarCreated(msg.sender, avatarId, username);
        return true;
    }
    
    function getAvatar(string memory avatarId) external view returns (
        string memory username,
        string memory email,
        string memory firstName,
        string memory lastName,
        string memory avatarType,
        string memory metadata
    ) {
        require(avatars[avatarId].exists, "Avatar does not exist");
        Avatar memory avatar = avatars[avatarId];
        return (
            avatar.username,
            avatar.email,
            avatar.firstName,
            avatar.lastName,
            avatar.avatarType,
            avatar.metadata
        );
    }
    
    function updateAvatar(
        string memory avatarId,
        string memory username,
        string memory email,
        string memory firstName,
        string memory lastName,
        string memory avatarType,
        string memory metadata
    ) external avatarExists(avatarId) returns (bool) {
        avatars[avatarId].username = username;
        avatars[avatarId].email = email;
        avatars[avatarId].firstName = firstName;
        avatars[avatarId].lastName = lastName;
        avatars[avatarId].avatarType = avatarType;
        avatars[avatarId].metadata = metadata;
        avatars[avatarId].modifiedDate = block.timestamp;
        
        emit AvatarUpdated(msg.sender, avatarId, username);
        return true;
    }
    
    function deleteAvatar(string memory avatarId) external avatarExists(avatarId) returns (bool) {
        delete avatars[avatarId];
        
        // Remove from user's avatar list
        string[] storage userAvatarList = userAvatars[msg.sender];
        for (uint256 i = 0; i < userAvatarList.length; i++) {
            if (keccak256(bytes(userAvatarList[i])) == keccak256(bytes(avatarId))) {
                userAvatarList[i] = userAvatarList[userAvatarList.length - 1];
                userAvatarList.pop();
                break;
            }
        }
        
        emit AvatarDeleted(msg.sender, avatarId);
        return true;
    }
    
    // Holon Functions
    function createHolon(
        string memory holonId,
        string memory name,
        string memory description,
        string memory holonType,
        string memory metadata,
        string memory parentId
    ) external returns (bool) {
        require(!holons[holonId].exists, "Holon already exists");
        
        holons[holonId] = Holon({
            id: holonId,
            name: name,
            description: description,
            holonType: holonType,
            metadata: metadata,
            parentId: parentId,
            createdDate: block.timestamp,
            modifiedDate: block.timestamp,
            exists: true
        });
        
        userHolons[msg.sender].push(holonId);
        
        emit HolonCreated(msg.sender, holonId, name);
        return true;
    }
    
    function getHolon(string memory holonId) external view returns (
        string memory name,
        string memory description,
        string memory holonType,
        string memory metadata,
        string memory parentId
    ) {
        require(holons[holonId].exists, "Holon does not exist");
        Holon memory holon = holons[holonId];
        return (
            holon.name,
            holon.description,
            holon.holonType,
            holon.metadata,
            holon.parentId
        );
    }
    
    function updateHolon(
        string memory holonId,
        string memory name,
        string memory description,
        string memory holonType,
        string memory metadata,
        string memory parentId
    ) external holonExists(holonId) returns (bool) {
        holons[holonId].name = name;
        holons[holonId].description = description;
        holons[holonId].holonType = holonType;
        holons[holonId].metadata = metadata;
        holons[holonId].parentId = parentId;
        holons[holonId].modifiedDate = block.timestamp;
        
        emit HolonUpdated(msg.sender, holonId, name);
        return true;
    }
    
    function deleteHolon(string memory holonId) external holonExists(holonId) returns (bool) {
        delete holons[holonId];
        
        // Remove from user's holon list
        string[] storage userHolonList = userHolons[msg.sender];
        for (uint256 i = 0; i < userHolonList.length; i++) {
            if (keccak256(bytes(userHolonList[i])) == keccak256(bytes(holonId))) {
                userHolonList[i] = userHolonList[userHolonList.length - 1];
                userHolonList.pop();
                break;
            }
        }
        
        emit HolonDeleted(msg.sender, holonId);
        return true;
    }
    
    // NFT Functions
    function mintNFT(
        string memory tokenURI,
        string memory metadata
    ) external returns (uint256) {
        uint256 tokenId = nextTokenId++;
        
        nfts[tokenId] = NFT({
            tokenId: tokenId,
            tokenURI: tokenURI,
            owner: msg.sender,
            metadata: metadata,
            createdDate: block.timestamp,
            exists: true
        });
        
        userNFTs[msg.sender].push(tokenId);
        
        emit NFTMinted(msg.sender, tokenId, tokenURI);
        return tokenId;
    }
    
    function getNFT(uint256 tokenId) external view returns (
        string memory tokenURI,
        address owner,
        string memory metadata
    ) {
        require(nfts[tokenId].exists, "NFT does not exist");
        NFT memory nft = nfts[tokenId];
        return (nft.tokenURI, nft.owner, nft.metadata);
    }
    
    function sendNFT(address to, uint256 tokenId) external nftExists(tokenId) returns (bool) {
        require(nfts[tokenId].owner == msg.sender, "Not the owner of this NFT");
        
        nfts[tokenId].owner = to;
        
        // Remove from sender's NFT list
        uint256[] storage senderNFTs = userNFTs[msg.sender];
        for (uint256 i = 0; i < senderNFTs.length; i++) {
            if (senderNFTs[i] == tokenId) {
                senderNFTs[i] = senderNFTs[senderNFTs.length - 1];
                senderNFTs.pop();
                break;
            }
        }
        
        // Add to recipient's NFT list
        userNFTs[to].push(tokenId);
        
        emit NFTSent(msg.sender, to, tokenId);
        return true;
    }
    
    // Utility Functions
    function getUserAvatars(address user) external view returns (string[] memory) {
        return userAvatars[user];
    }
    
    function getUserHolons(address user) external view returns (string[] memory) {
        return userHolons[user];
    }
    
    function getUserNFTs(address user) external view returns (uint256[] memory) {
        return userNFTs[user];
    }
    
    function getAvatarCount() external view returns (uint256) {
        return userAvatars[msg.sender].length;
    }
    
    function getHolonCount() external view returns (uint256) {
        return userHolons[msg.sender].length;
    }
    
    function getNFTCount() external view returns (uint256) {
        return userNFTs[msg.sender].length;
    }
}
