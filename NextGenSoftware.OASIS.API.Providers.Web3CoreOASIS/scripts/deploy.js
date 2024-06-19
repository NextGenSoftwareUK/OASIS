async function main() {
    const Web3CoreOASIS = await ethers.getContractFactory("Web3CoreOASIS");
    const web3CoreOASIS = await Web3CoreOASIS.deploy();
    await web3CoreOASIS.deployed();
    console.log("Web3CoreOASIS deployed to:", web3CoreOASIS.address);
}

main()
    .then(() => process.exit(0))
    .catch((error) => {
        console.error(error);
        process.exit(1);
    });
