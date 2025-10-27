const { expect } = require("chai");
const { ethers } = require("hardhat");

describe("Web3CoreOASIS Performance Tests", function () {
    let contract;
    let owner;

    before(async () => {
        const Web3CoreOASIS = await ethers.getContractFactory("Web3CoreOASIS");
        [owner] = await ethers.getSigners();
        contract = await Web3CoreOASIS.deploy();
    });

    it("should measure performance of createAvatar", async function () {
        const avatarId = ethers.encodeBytes32String("Avatar1");
        const info = ethers.encodeBytes32String("Info1");

        const startTime = process.hrtime();
        await contract.createAvatar(1, avatarId, info);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for createAvatar: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of createHolon", async function () {
        const holonId = ethers.encodeBytes32String("Holon1");
        const info = ethers.encodeBytes32String("Info1");

        const startTime = process.hrtime();
        await contract.createHolon(1, holonId, info);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for createHolon: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of createAvatarDetail", async function () {
        const avatarId = ethers.encodeBytes32String("AvatarDetail1");
        const info = ethers.encodeBytes32String("Info1");

        const startTime = process.hrtime();
        await contract.createAvatarDetail(1, avatarId, info);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for createAvatarDetail: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of updateAvatar", async function () {
        const newInfo = ethers.encodeBytes32String("UpdatedInfo1");

        const startTime = process.hrtime();
        await contract.updateAvatar(1, newInfo);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for updateAvatar: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of updateAvatarDetail", async function () {
        const newInfo = ethers.encodeBytes32String("UpdatedInfo1");

        const startTime = process.hrtime();
        await contract.updateAvatarDetail(1, newInfo);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for updateAvatarDetail: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of updateHolon", async function () {
        const newInfo = ethers.encodeBytes32String("UpdatedInfo1");

        const startTime = process.hrtime();
        await contract.updateHolon(1, newInfo);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for updateHolon: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of deleteAvatar", async function () {
        const startTime = process.hrtime();
        await contract.deleteAvatar(1);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for deleteAvatar: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of deleteAvatarDetail", async function () {
        const startTime = process.hrtime();
        await contract.deleteAvatarDetail(1);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for deleteAvatarDetail: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of deleteHolon", async function () {
        const startTime = process.hrtime();
        await contract.deleteHolon(1);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for deleteHolon: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of getAvatarById", async function () {
        const entityId = 2;

        const avatarId = ethers.encodeBytes32String(`Avatar${entityId}`);
        const info = ethers.encodeBytes32String(`Info${entityId}`);

        await contract.createAvatar(entityId, avatarId, info);

        const startTime = process.hrtime();
        await contract.getAvatarById(entityId);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for getAvatarById: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of getAvatarDetailById", async function () {
        const entityId = 2;

        const avatarId = ethers.encodeBytes32String(`AvatarDetail${entityId}`);
        const info = ethers.encodeBytes32String(`Info${entityId}`);

        await contract.createAvatarDetail(entityId, avatarId, info);

        const startTime = process.hrtime();
        await contract.getAvatarDetailById(entityId);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for getAvatarDetailById: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of getHolonById", async function () {
        const entityId = 2;

        const holonId = ethers.encodeBytes32String("Holon1");
        const info = ethers.encodeBytes32String("Info1");

        await contract.createHolon(entityId, holonId, info);

        const startTime = process.hrtime();
        await contract.getHolonById(entityId);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for getHolonById: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of getAvatarsCount", async function () {
        const startTime = process.hrtime();
        await contract.getAvatarsCount();
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for getAvatarsCount: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of getAvatarDetailsCount", async function () {
        const startTime = process.hrtime();
        await contract.getAvatarDetailsCount();
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for getAvatarDetailsCount: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure performance of getHolonsCount", async function () {
        const startTime = process.hrtime();
        await contract.getHolonsCount();
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for getHolonsCount: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });

    it("should measure average performance of createAvatar", async function () {
        const avatarId = ethers.encodeBytes32String("Avatar1");
        const info = ethers.encodeBytes32String("Info1");

        let totalTime = [0, 0];
        const iterations = 10;
        const entityIdOffset = 100;

        for (let i = 0; i < iterations; i++) {
            const startTime = process.hrtime();
            await contract.createAvatar(i + entityIdOffset, avatarId, info);
            const endTime = process.hrtime(startTime);
            totalTime[0] += endTime[0];
            totalTime[1] += endTime[1];
        }

        console.log(
            `Average execution time for createAvatar: ${
                totalTime[0] / iterations
            }s ${totalTime[1] / iterations / 1000000}ms`
        );
    });
});
