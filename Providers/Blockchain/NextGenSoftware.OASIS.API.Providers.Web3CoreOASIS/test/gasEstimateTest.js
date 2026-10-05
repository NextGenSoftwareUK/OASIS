const { expect } = require("chai");
const { ethers } = require("hardhat");

describe("Web3CoreOASIS GAS Estimate Tests", function () {
    let contract;
    let owner;

    before(async () => {
        const Web3CoreOASIS = await ethers.getContractFactory("Web3CoreOASIS");
        [owner] = await ethers.getSigners();
        contract = await Web3CoreOASIS.deploy();
    });

    it("should measure gas usage for createAvatar", async function () {
        const avatarId = ethers.encodeBytes32String("Avatar1");
        const info = ethers.encodeBytes32String("Info1");

        const tx = await contract.createAvatar(1, avatarId, info);
        const receipt = await tx.wait();

        console.log("Gas used for createAvatar:", receipt.gasUsed.toString());
    });

    it("should measure gas usage for createHolon", async function () {
        const holonId = ethers.encodeBytes32String("Holon1");
        const info = ethers.encodeBytes32String("Info1");

        const tx = await contract.createHolon(1, holonId, info);
        const receipt = await tx.wait();

        console.log("Gas used for createHolon:", receipt.gasUsed.toString());
    });

    it("should measure gas usage for createAvatarDetail", async function () {
        const avatarId = ethers.encodeBytes32String("AvatarDetail1");
        const info = ethers.encodeBytes32String("Info1");

        const tx = await contract.createAvatarDetail(1, avatarId, info);
        const receipt = await tx.wait();

        console.log(
            "Gas used for createAvatarDetail:",
            receipt.gasUsed.toString()
        );
    });

    it("should measure gas usage for updateAvatar", async function () {
        const newInfo = ethers.encodeBytes32String("UpdatedInfo1");

        const tx = await contract.updateAvatar(1, newInfo);
        const receipt = await tx.wait();

        console.log("Gas used for updateAvatar:", receipt.gasUsed.toString());
    });

    it("should measure gas usage for updateAvatarDetail", async function () {
        const newInfo = ethers.encodeBytes32String("UpdatedInfo1");

        const tx = await contract.updateAvatarDetail(1, newInfo);
        const receipt = await tx.wait();

        console.log(
            "Gas used for updateAvatarDetail:",
            receipt.gasUsed.toString()
        );
    });

    it("should measure gas usage for updateHolon", async function () {
        const newInfo = ethers.encodeBytes32String("UpdatedInfo1");

        const tx = await contract.updateHolon(1, newInfo);
        const receipt = await tx.wait();

        console.log("Gas used for updateHolon:", receipt.gasUsed.toString());
    });

    it("should measure gas usage for deleteAvatar", async function () {
        const tx = await contract.deleteAvatar(1);
        const receipt = await tx.wait();

        console.log("Gas used for deleteAvatar:", receipt.gasUsed.toString());
    });

    it("should measure gas usage for deleteAvatarDetail", async function () {
        const tx = await contract.deleteAvatarDetail(1);
        const receipt = await tx.wait();

        console.log(
            "Gas used for deleteAvatarDetail:",
            receipt.gasUsed.toString()
        );
    });

    it("should measure gas usage for deleteHolon", async function () {
        const tx = await contract.deleteHolon(1);
        const receipt = await tx.wait();

        console.log("Gas used for deleteHolon:", receipt.gasUsed.toString());
    });

    it("should measure gas usage for getAvatarById", async function () {
        const entityId = 2;

        const avatarId = ethers.encodeBytes32String(`Avatar${entityId}`);
        const info = ethers.encodeBytes32String(`Info${entityId}`);

        await contract.createAvatar(entityId, avatarId, info);

        const tx = await contract.getAvatarById(entityId);

        console.log("Gas used for getAvatarById:", tx);
    });

    it("should measure gas usage for getAvatarDetailById", async function () {
        const entityId = 2;

        const avatarId = ethers.encodeBytes32String(`AvatarDetail${entityId}`);
        const info = ethers.encodeBytes32String(`Info${entityId}`);

        await contract.createAvatarDetail(entityId, avatarId, info);

        const tx = await contract.getAvatarDetailById(entityId);

        console.log("Gas used for getAvatarDetailById:", tx);
    });

    it("should measure gas usage for getHolonById", async function () {
        const entityId = 2;

        const holonId = ethers.encodeBytes32String("Holon1");
        const info = ethers.encodeBytes32String("Info1");

        await contract.createHolon(entityId, holonId, info);

        const tx = await contract.getHolonById(entityId);

        console.log("Gas used for getHolonById:", tx);
    });

    it("should measure gas usage for getAvatarsCount", async function () {
        const tx = await contract.getAvatarsCount();

        console.log("Gas used for getAvatarsCount:", tx);
    });

    it("should measure gas usage for getAvatarDetailsCount", async function () {
        const tx = await contract.getAvatarDetailsCount();

        console.log("Gas used for getAvatarDetailsCount:", tx);
    });

    it("should measure gas usage for getHolonsCount", async function () {
        const tx = await contract.getHolonsCount();

        console.log("Gas used for getHolonsCount:", tx);
    });
});
