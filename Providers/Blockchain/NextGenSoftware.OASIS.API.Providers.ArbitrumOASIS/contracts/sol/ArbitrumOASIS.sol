// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.20;

import "./@openzeppelin/contracts/token/ERC721/ERC721.sol";
import "./@openzeppelin/contracts/access/Ownable.sol";

import "./AvatarDetail.sol";
import "./Avatar.sol";
import "./Holon.sol";

contract ArbitrumOASIS is ERC721, Ownable {
    
    Avatar[] private avatars;
    AvatarDetail[] private avatarDetails;
    Holon[] private holons;
    
    uint256 private totalAvatarsCount;
    uint256 private totalAvatarDetailsCount;
    uint256 private totalHolonsCount;

    struct NFTMetadata {
        string metadataUri;
    }

    struct NFTTransfer {
        address fromWalletAddress;
        address toWalletAddress;
        string fromProviderType;
        string toProviderType;
        uint256 amount;
        string memoText;
    }

    mapping(uint256 => NFTMetadata) public nftMetadata;
    mapping(uint256 => NFTTransfer[]) public nftTransfers;

    uint256 public nextTokenId;
    address public admin;

    constructor() ERC721('ArbitrumOASIS', 'MNFT') Ownable(msg.sender) {
        admin = msg.sender;
        totalAvatarsCount = 0;
        totalAvatarDetailsCount = 0;
        totalHolonsCount = 0;
    }

    function CreateAvatar(
        uint256 entityId,
        string memory avatarId,
        string memory info
    ) public returns (uint256) {
        Avatar memory newAvatar = Avatar({
            AvatarId: avatarId,
            EntityId: entityId,
            Info: info
        });
        avatars.push(newAvatar);
        totalAvatarsCount++;
        return totalAvatarsCount;
    }

    function CreateHolon(
        uint256 entityId,
        string memory holonId,
        string memory info
    ) public returns (uint256) {
        Holon memory newHolon = Holon(
        {
            EntityId: entityId,
            HolonId: holonId,
            Info: info
        });
        holons.push(newHolon);
        totalHolonsCount++;
        return totalHolonsCount;
    }

    function CreateAvatarDetail(
        uint256 entityId,
        string memory avatarId,
        string memory info
    ) public returns (uint256) {
        AvatarDetail memory newAvatarDetail = AvatarDetail(
        {
            AvatarId: avatarId,
            EntityId: entityId,
            Info: info
        });
        avatarDetails.push(newAvatarDetail);
        totalAvatarDetailsCount++;
        return totalAvatarDetailsCount;
    }

    function UpdateAvatar(
        uint256 entityId,
        string memory info
    ) public returns (bool) {
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].EntityId == entityId) {
                avatars[i].Info = info;
                return true;
            }
        }
        return false;
    }

    function UpdateAvatarDetail(
        uint256 entityId,
        string memory info
    ) public returns (bool) {
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].EntityId == entityId) {
                avatarDetails[i].Info = info;
                return true;
            }
        }
        return false;
    }

    function UpdateHolon(
        uint256 entityId,
        string memory info
    ) public returns (bool) {
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (holons[i].EntityId == entityId) {
                holons[i].Info = info;
                return true;
            }
        }
        return false;
    }

    function DeleteAvatar(uint256 entityId) public returns (bool) {
        require(totalAvatarsCount > 0);
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].EntityId == entityId) {
                avatars[i] = avatars[totalAvatarsCount - 1];
                delete avatars[totalAvatarsCount - 1];
                totalAvatarsCount--;
                return true;
            }
        }
        return false;
    }
    
    function DeleteAvatarDetail(uint256 entityId) public returns (bool) {
        require(totalAvatarDetailsCount > 0);
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].EntityId == entityId) {
                avatarDetails[i] = avatarDetails[totalAvatarDetailsCount - 1];
                delete avatarDetails[totalAvatarDetailsCount - 1];
                totalAvatarDetailsCount--;
                return true;
            }
        }
        return false;
    }
    
    function DeleteHolon(uint256 entityId) public returns (bool) {
        require(totalHolonsCount > 0);
        for (uint256 i = 0; i < totalHolonsCount; i++) {
            if (holons[i].EntityId == entityId) {
                holons[i] = holons[totalHolonsCount - 1];
                delete holons[totalHolonsCount - 1];
                totalHolonsCount--;
                return true;
            }
        }
        return false;
    }
    
    function GetAvatarById(uint256 entityId) public view returns (Avatar memory)
    {
        Avatar memory avatar;
        require(totalAvatarsCount > 0);
        for (uint256 i = 0; i < totalAvatarsCount; i++) {
            if (avatars[i].EntityId == entityId) {
                return avatars[i];
            }
        }
        return avatar;
    }

    function GetAvatarDetailById(uint256 entityId) public view returns (AvatarDetail memory)
    {
        AvatarDetail memory avatarDetail;
        require(totalAvatarDetailsCount > 0);
        for (uint256 i = 0; i < totalAvatarDetailsCount; i++) {
            if (avatarDetails[i].EntityId == entityId) {
                return avatarDetails[i];
            }
        }
        return avatarDetail;
    }

    function GetHolonById(uint256 entityId) public view returns (Holon memory)
    {
        Holon memory holon;
        require(totalHolonsCount > 0);
        for (uint256 i = 0; i < totalHolonsCount; i++) {
            if (holons[i].EntityId == entityId) {
                return holons[i];
            }
        }
        return holon;
    }
    
    function GetAvatarsCount() public view returns (uint256 count) {
        return avatars.length;
    }

    function GetAvatarDetailsCount() public view returns (uint256 count) {
        return avatarDetails.length;
    }

    function GetHolonsCount() public view returns (uint256 count) {
        return holons.length;
    }

    function mint(
        address to,
        string memory metadataUri
    ) external onlyOwner {
        nftMetadata[nextTokenId] = NFTMetadata(
            metadataUri
        );

        _safeMint(to, nextTokenId);
        nextTokenId++;
    }

    function sendNFT(
        address fromWalletAddress,
        address toWalletAddress,
        uint256 tokenId,
        string memory fromProviderType,
        string memory toProviderType,
        uint256 amount,
        string memory memoText
    ) external {
        require(ownerOf(tokenId) == fromWalletAddress, "You are not the owner of this token");
        require(fromWalletAddress == msg.sender, "You are not authorized to send this token");

        _transfer(fromWalletAddress, toWalletAddress, tokenId);

        NFTTransfer memory transfer = NFTTransfer({
            fromWalletAddress: fromWalletAddress,
            toWalletAddress: toWalletAddress,
            fromProviderType: fromProviderType,
            toProviderType: toProviderType,
            amount: amount,
            memoText: memoText
        });

        nftTransfers[tokenId].push(transfer);
    }

    function getTransferHistory(uint256 tokenId) external view returns (NFTTransfer[] memory) {
        return nftTransfers[tokenId];
    }

    function tokenExists(uint256 tokenId) public view returns (bool) {
        return bytes(nftMetadata[tokenId].metadataUri).length > 0;
    }

    function tokenURI(uint256 tokenId) public view override returns (string memory) {
        require(tokenExists(tokenId), "ERC721Metadata: URI query for nonexistent token");

        return nftMetadata[tokenId].metadataUri;
    }
}