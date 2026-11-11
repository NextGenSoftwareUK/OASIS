require("@nomicfoundation/hardhat-toolbox");

const configuredAccounts = process.env.PRIVATE_KEY ? [process.env.PRIVATE_KEY] : [];
const localChainId = Number(process.env.LOCAL_CHAIN_ID || "1337");

module.exports = {
    solidity: {
        version: "0.8.20",
        settings: {
            evmVersion: "paris",
            optimizer: { enabled: true, runs: 200 },
        },
    },
    paths: {
        sources: "./Contracts",
    },
    networks: {
        hardhat: {
            chainId: localChainId,
        },
        localhost: {
            url: "http://127.0.0.1:8545",
            chainId: localChainId,
        },
        rootstock: {
            url: "https://public-node.testnet.rsk.co",
            accounts: configuredAccounts,
        },
        polygon: {
            url: "https://rpc-amoy.polygon.technology/",
            accounts: configuredAccounts,
        },
        arbitrum: {
            url: "https://sepolia-rollup.arbitrum.io/rpc",
            chainId: 421614,
            accounts: configuredAccounts,
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
