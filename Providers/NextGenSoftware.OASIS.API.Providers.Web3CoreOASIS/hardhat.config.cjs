require("@nomicfoundation/hardhat-toolbox");

module.exports = {
    solidity: "0.8.20",
    networks: {
        hardhat: {
            chainId: 1337,
        },
        localhost: {
            url: "http://127.0.0.1:8545",
            chainId: 1337,
        },
        rootstock: {
            url: "https://public-node.testnet.rsk.co",
            accounts: [process.env.PRIVATE_KEY],
        },
        polygon: {
            url: "https://rpc-amoy.polygon.technology/",
            accounts: [process.env.PRIVATE_KEY],
        },
        arbitrum: {
            url: "https://sepolia-rollup.arbitrum.io/rpc",
            chainId: 421614,
            accounts: [process.env.PRIVATE_KEY],
        },
    },
    etherscan: {
        apiKey: process.env.SCAN_API_KEY,
    },
    type: "module",
};

task("accounts", "Prints the list of accounts", async (_, { ethers }) => {
    const accounts = await ethers.getSigners();
    for (const account of accounts) {
        console.log(account.address);
    }
});
