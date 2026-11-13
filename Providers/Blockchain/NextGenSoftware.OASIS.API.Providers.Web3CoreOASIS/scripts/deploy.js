const { ethers } = require("hardhat");

async function main() {
    const Web3CoreOASIS = await ethers.getContractFactory("Web3CoreOASIS");
    const deployedContract = await Web3CoreOASIS.deploy();
    await deployedContract.waitForDeployment();
    const address = await deployedContract.getAddress();
    if (await ethers.provider.getCode(address) === "0x") {
        throw new Error("Web3CoreOASIS deployment has no bytecode at the confirmed address.");
    }
    console.log("Web3CoreOASIS deployed to:", address);
}

main().catch((error) => {
    console.error(error);
    process.exitCode = 1;
});
