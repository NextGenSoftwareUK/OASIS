const { expect } = require("chai");
const { ethers } = require("hardhat");

describe("Web3CoreOASIS Tests", function () {
    let contract;
    let owner, addr1, addr2;

    before(async () => {
        const Web3CoreOASIS = await ethers.getContractFactory("Web3CoreOASIS");
        [owner, addr1, addr2] = await ethers.getSigners();
        contract = await Web3CoreOASIS.deploy();
    });

    it("should create an avatar", async function () {
        const avatarId = ethers.encodeBytes32String("Avatar1");
        const info = ethers.encodeBytes32String("Info1");

        await contract.createAvatar(1, avatarId, info);
        const avatar = await contract.getAvatarById(1);

        expect(avatar.ExternalId).to.equal(avatarId);
    });

    it("should create a holon", async function () {
        const holonId = ethers.encodeBytes32String("Holon1");
        const info = ethers.encodeBytes32String("Info1");

        await contract.createHolon(1, holonId, info);
        const holon = await contract.getHolonById(1);

        expect(holon.ExternalId).to.equal(holonId);
    });

    it("should create an avatar detail", async function () {
        const avatarId = ethers.encodeBytes32String("AvatarDetail1");
        const info = ethers.encodeBytes32String("Info1");

        await contract.createAvatarDetail(1, avatarId, info);
        const avatarDetail = await contract.getAvatarDetailById(1);

        expect(avatarDetail.ExternalId).to.equal(avatarId);
    });

    it("should update an avatar", async function () {
        const newInfo = ethers.encodeBytes32String("UpdatedInfo1");

        await contract.updateAvatar(1, newInfo);
        const avatar = await contract.getAvatarById(1);

        expect(avatar.Info).to.equal(newInfo);
    });

    it("should update an avatar detail", async function () {
        const newInfo = ethers.encodeBytes32String("UpdatedInfo1");

        await contract.updateAvatarDetail(1, newInfo);
        const avatarDetail = await contract.getAvatarDetailById(1);

        expect(avatarDetail.Info).to.equal(newInfo);
    });

    it("should update a holon", async function () {
        const newInfo = ethers.encodeBytes32String("UpdatedInfo1");

        await contract.updateHolon(1, newInfo);
        const holon = await contract.getHolonById(1);

        expect(holon.Info).to.equal(newInfo);
    });

    it("should delete an avatar", async function () {
        await contract.deleteAvatar(1);
        await expect(contract.getAvatarById(1)).to.be.revertedWith(
            "Entity not exist"
        );
    });

    it("should delete an avatar detail", async function () {
        await contract.deleteAvatarDetail(1);
        await expect(contract.getAvatarDetailById(1)).to.be.revertedWith(
            "Entity not exist"
        );
    });

    it("should delete a holon", async function () {
        await contract.deleteHolon(1);
        await expect(contract.getHolonById(1)).to.be.revertedWith(
            "Entity not exist"
        );
    });

    it("should get the correct avatars count", async function () {
        const count = await contract.getAvatarsCount();
        expect(count).to.equal(0);
    });

    it("should get the correct avatar details count", async function () {
        const count = await contract.getAvatarDetailsCount();
        expect(count).to.equal(0);
    });

    it("should get the correct holons count", async function () {
        const count = await contract.getHolonsCount();
        expect(count).to.equal(0);
    });

    it("should mint an NFT", async function () {
        const metadataJson = JSON.stringify({ name: "NFT1", description: "My first NFT" });
        await contract.mint(addr1.address, metadataJson);

        const tokenId = 0;
        const ownerOfToken = await contract.ownerOf(tokenId);

        expect(ownerOfToken).to.equal(addr1.address);
    });

    it("should transfer an NFT between two addresses", async function () {
        const metadataJson = JSON.stringify({ name: "NFT2", description: "My second NFT" });
        await contract.mint(addr1.address, metadataJson);

        const tokenId = 1;

        const fromProviderType = "provider1";
        const toProviderType = "provider2";
        const amount = 1;
        const memoText = "Transfer memo";

        await contract.connect(addr1).sendNFT(
            addr1.address,
            addr2.address,
            tokenId,
            fromProviderType,
            toProviderType,
            amount,
            memoText
        );

        const newOwner = await contract.ownerOf(tokenId);
        expect(newOwner).to.equal(addr2.address);
    });
});
