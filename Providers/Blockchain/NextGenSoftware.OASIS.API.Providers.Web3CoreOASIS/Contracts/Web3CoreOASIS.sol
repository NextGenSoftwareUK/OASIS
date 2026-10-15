// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.20;

import "./@openzeppelin/contracts/token/ERC721/ERC721.sol";
import "./@openzeppelin/contracts/access/Ownable.sol";
import "./@openzeppelin/contracts/utils/structs/EnumerableSet.sol";

import "./IWeb3CoreOASIS.sol";
import "./EntityOASIS.sol";
import "./NFTMetadata.sol";
import "./NFTTransfer.sol";

contract Web3CoreOASIS is ERC721, Ownable, IWeb3CoreOASIS {
    using EnumerableSet for EnumerableSet.UintSet;
    EnumerableSet.UintSet private avatarIds;
    EnumerableSet.UintSet private avatarDetailIds;
    EnumerableSet.UintSet private holonIds;

    string constant ENTITY_NOT_EXIST = "Entity not exist";
    string constant ENTITY_ALREADY_EXIST = "Entity already exist";
    string constant OWNER_NOT_AUTHORIZED = "Not authorized";
    string constant OWNER_NOT_TOKEN = "You are not the owner of this token";

    mapping(uint256 => EntityOASIS) private avatars;
    mapping(uint256 => EntityOASIS) private avatarDetails;
    mapping(uint256 => EntityOASIS) private holons;
    mapping(uint256 => NFTMetadata) public nftMetadata;
    mapping(uint256 => NFTTransfer[]) public nftTransfers;

    uint256 private totalAvatarsCount;
    uint256 private totalAvatarDetailsCount;
    uint256 private totalHolonsCount;
    uint256 public nextTokenId;

    address private admin;

    function applyAvatarLifecycle(AvatarLifecycleRequest memory request) external onlyOwner returns (bool) {
        uint256 id = request.entityId;
        require(request.externalId != bytes32(0) && avatars[id].ExternalId == request.externalId, "Avatar identity changed");
        require(keccak256(avatars[id].Info) == request.expectedAvatarHash, "Avatar payload changed");
        bool hasDetail = avatarDetails[id].ExternalId != bytes32(0);
        require(hasDetail == request.hasDetail, "Avatar detail presence changed");
        if (hasDetail) {
            require(avatarDetails[id].ExternalId == request.externalId, "Avatar detail identity changed");
            require(keccak256(avatarDetails[id].Info) == request.expectedDetailHash, "Avatar detail payload changed");
        } else {
            require(request.expectedDetailHash == bytes32(0) && request.detailInfo.length == 0, "Unexpected avatar detail payload");
        }
        if (request.hardDelete) {
            delete avatars[id]; avatarIds.remove(id); totalAvatarsCount--;
            if (hasDetail) {delete avatarDetails[id]; avatarDetailIds.remove(id); totalAvatarDetailsCount--;}
        } else {
            require(request.avatarInfo.length > 0, "Avatar tombstone required");
            if (hasDetail) require(request.detailInfo.length > 0, "Avatar detail tombstone required");
            avatars[id].Info = request.avatarInfo;
            if (hasDetail) avatarDetails[id].Info = request.detailInfo;
        }
        return true;
    }

    constructor() ERC721('Web3CoreOASIS', 'MNFT') Ownable(msg.sender) {
        admin = msg.sender;
        totalAvatarsCount = 0;
        totalAvatarDetailsCount = 0;
        totalHolonsCount = 0;
    }

    function createAvatar(
        uint256 entityId,
        bytes32 avatarId,
        bytes memory info
    ) public onlyOwner returns (uint256) {
        require(avatars[entityId].ExternalId == bytes32(0), ENTITY_ALREADY_EXIST);
        require(avatarId != bytes32(0), "External identity required");
        avatars[entityId] = EntityOASIS(entityId, avatarId, info);
        avatarIds.add(entityId);
        totalAvatarsCount++;
        return totalAvatarsCount;
    }

    function createHolon(
        uint256 entityId,
        bytes32 holonId,
        bytes memory info
    ) public onlyOwner returns (uint256) {
        require(holons[entityId].ExternalId == bytes32(0), ENTITY_ALREADY_EXIST);
        require(holonId != bytes32(0), "External identity required");
        holons[entityId] = EntityOASIS(entityId, holonId, info);
        holonIds.add(entityId);
        totalHolonsCount++;
        return totalHolonsCount;
    }

    function createAvatarDetail(
        uint256 entityId,
        bytes32 avatarId,
        bytes memory info
    ) public onlyOwner returns (uint256) {
        require(avatarDetails[entityId].ExternalId == bytes32(0), ENTITY_ALREADY_EXIST);
        require(avatarId != bytes32(0), "External identity required");
        avatarDetails[entityId] = EntityOASIS(entityId, avatarId, info);
        avatarDetailIds.add(entityId);
        totalAvatarDetailsCount++;
        return totalAvatarDetailsCount;
    }

    function updateAvatar(
        uint256 entityId,
        bytes memory info
    ) public onlyOwner returns (bool) {
        require(avatars[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        avatars[entityId].Info = info;
        return true;
    }

    function upsertAvatar(uint256 entityId, bytes32 externalId, bytes memory info) external onlyOwner returns (bool) {
        if (upsert(avatars, avatarIds, entityId, externalId, info)) totalAvatarsCount++;
        return true;
    }

    function upsertAvatarDetail(uint256 entityId, bytes32 externalId, bytes memory info) external onlyOwner returns (bool) {
        if (upsert(avatarDetails, avatarDetailIds, entityId, externalId, info)) totalAvatarDetailsCount++;
        return true;
    }

    function upsertHolon(uint256 entityId, bytes32 externalId, bytes memory info) external onlyOwner returns (bool) {
        if (upsert(holons, holonIds, entityId, externalId, info)) totalHolonsCount++;
        return true;
    }

    function upsertAvatarChecked(uint256 entityId, bytes32 externalId, bool expectedPresent, bytes32 expectedHash, bytes memory info) external onlyOwner returns (bool) {
        checkExpected(avatars[entityId], expectedPresent, expectedHash);
        if (upsert(avatars, avatarIds, entityId, externalId, info)) totalAvatarsCount++;
        return true;
    }

    function upsertAvatarDetailChecked(uint256 entityId, bytes32 externalId, bool expectedPresent, bytes32 expectedHash, bytes memory info) external onlyOwner returns (bool) {
        checkExpected(avatarDetails[entityId], expectedPresent, expectedHash);
        if (upsert(avatarDetails, avatarDetailIds, entityId, externalId, info)) totalAvatarDetailsCount++;
        return true;
    }

    function upsertHolonChecked(uint256 entityId, bytes32 externalId, bool expectedPresent, bytes32 expectedHash, bytes memory info) external onlyOwner returns (bool) {
        checkExpected(holons[entityId], expectedPresent, expectedHash);
        if (upsert(holons, holonIds, entityId, externalId, info)) totalHolonsCount++;
        return true;
    }

    function checkExpected(EntityOASIS storage record, bool expectedPresent, bytes32 expectedHash) private view {
        require((record.ExternalId != bytes32(0)) == expectedPresent, "Storage presence conflict");
        require(keccak256(record.Info) == expectedHash, "Storage payload conflict");
    }

    function upsert(mapping(uint256 => EntityOASIS) storage records, EnumerableSet.UintSet storage ids,
        uint256 entityId, bytes32 externalId, bytes memory info) private returns (bool created) {
        require(externalId != bytes32(0), "External identity required");
        created = records[entityId].ExternalId == bytes32(0);
        if (created) {
            require(ids.add(entityId), "Inconsistent entity index");
            records[entityId] = EntityOASIS(entityId, externalId, info);
        } else {
            require(records[entityId].ExternalId == externalId, "Storage identity collision");
            records[entityId].Info = info;
        }
    }

    function updateAvatarDetail(
        uint256 entityId,
        bytes memory info
    ) public onlyOwner returns (bool) {
        require(avatarDetails[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        avatarDetails[entityId].Info = info;
        return true;
    }

    function updateHolon(
        uint256 entityId,
        bytes memory info
    ) public onlyOwner returns (bool) {
        require(holons[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        holons[entityId].Info = info;
        return true;
    }

    function deleteAvatar(uint256 entityId) public onlyOwner returns (bool) {
        require(avatars[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        delete avatars[entityId];
        avatarIds.remove(entityId);
        totalAvatarsCount--;
        return true;
    }

    function deleteAvatarDetail(uint256 entityId) public onlyOwner returns (bool) {
        require(avatarDetails[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        delete avatarDetails[entityId];
        avatarDetailIds.remove(entityId);
        totalAvatarDetailsCount--;
        return true;
    }

    function deleteHolonChecked(uint256 entityId, bytes32 externalId, bytes32 expectedHash) external onlyOwner returns (bool) {
        require(externalId != bytes32(0) && holons[entityId].ExternalId == externalId, "Holon identity conflict");
        require(keccak256(holons[entityId].Info) == expectedHash, "Holon payload conflict");
        return deleteHolon(entityId);
    }

    function deleteHolon(uint256 entityId) public onlyOwner returns (bool) {
        require(holons[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        delete holons[entityId];
        holonIds.remove(entityId);
        totalHolonsCount--;
        return true;
    }

    function getAvatarById(uint256 entityId) public view returns (EntityOASIS memory) {
        require(avatars[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        return avatars[entityId];
    }

    // The all-zero mapping value is structured absence; transport/revert errors stay errors.
    function lookupAvatar(uint256 entityId) external view returns (EntityOASIS memory) { return avatars[entityId]; }
    function lookupAvatarDetail(uint256 entityId) external view returns (EntityOASIS memory) { return avatarDetails[entityId]; }
    function lookupHolon(uint256 entityId) external view returns (EntityOASIS memory) { return holons[entityId]; }

    function getAvatarDetailById(uint256 entityId) public view returns (EntityOASIS memory) {
        require(avatarDetails[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        return avatarDetails[entityId];
    }

    function getHolonById(uint256 entityId) public view returns (EntityOASIS memory) {
        require(holons[entityId].ExternalId != bytes32(0), ENTITY_NOT_EXIST);
        return holons[entityId];
    }

    function getAvatarsCount() public view returns (uint256 count) {
        return totalAvatarsCount;
    }

    // Offset pages must be read at one block: delete uses swap-and-pop indexing.
    function getAvatarsPage(uint256 offset, uint256 limit) external view returns (EntityOASIS[] memory) {
        return page(avatars, avatarIds, offset, limit);
    }

    function getAvatarDetailsPage(uint256 offset, uint256 limit) external view returns (EntityOASIS[] memory) {
        return page(avatarDetails, avatarDetailIds, offset, limit);
    }

    function getHolonsPage(uint256 offset, uint256 limit) external view returns (EntityOASIS[] memory) {
        return page(holons, holonIds, offset, limit);
    }

    function page(mapping(uint256 => EntityOASIS) storage records, EnumerableSet.UintSet storage ids,
        uint256 offset, uint256 limit) private view returns (EntityOASIS[] memory result) {
        require(limit > 0 && limit <= 256, "Page limit must be 1..256");
        uint256 count = ids.length();
        if (offset >= count) return new EntityOASIS[](0);
        uint256 length = count - offset;
        if (length > limit) length = limit;
        result = new EntityOASIS[](length);
        for (uint256 i = 0; i < length; i++) result[i] = records[ids.at(offset + i)];
    }

    function getAvatarDetailsCount() public view returns (uint256 count) {
        return totalAvatarDetailsCount;
    }

    function getHolonsCount() public view returns (uint256 count) {
        return totalHolonsCount;
    }

    function mint(
        address to,
        string memory metadataJson
    ) external onlyOwner {
        nftMetadata[nextTokenId] = NFTMetadata(
            metadataJson
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
        require(ownerOf(tokenId) == fromWalletAddress, OWNER_NOT_TOKEN);
        require(fromWalletAddress == msg.sender, OWNER_NOT_AUTHORIZED);

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
}
