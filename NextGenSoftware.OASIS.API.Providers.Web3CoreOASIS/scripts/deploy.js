async function main() {
    const Web3CoreOASIS = await ethers.getContractFactory("Web3CoreOASIS");
    const deployedContract = await Web3CoreOASIS.deploy();
    console.log("Web3CoreOASIS deployed to:", deployedContract.target);
}

main()
    .then(() => process.exit(0))
    .catch((error) => {
        console.error(error);
        process.exit(1);
    });
