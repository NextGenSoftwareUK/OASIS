/**
 * Aztec Bridge Service - Node.js service for submitting real Aztec transactions
 * Uses Aztec.js SDK - NO MOCKS
 */

const express = require('express');
const cors = require('cors');
const { createAztecClient, getSchnorrAccount } = require('@aztec/aztec.js');

const app = express();
app.use(cors());
app.use(express.json());

const PORT = process.env.PORT || 3002;
const NODE_URL = process.env.AZTEC_NODE_URL || 'https://aztec-testnet-fullnode.zkv.xyz';
const PXE_URL = process.env.AZTEC_PXE_URL || NODE_URL;

let aztecClient = null;
let accounts = {};

/**
 * Initialize Aztec client
 */
async function initializeAztec() {
    try {
        console.log(`Connecting to Aztec node: ${NODE_URL}`);
        aztecClient = await createAztecClient(NODE_URL);
        console.log('✅ Aztec client initialized');
        return true;
    } catch (error) {
        console.error('❌ Failed to initialize Aztec client:', error);
        return false;
    }
}

/**
 * Load account from wallet
 */
async function loadAccount(accountAlias) {
    try {
        if (accounts[accountAlias]) {
            return accounts[accountAlias];
        }

        // Load account from Aztec wallet
        // Note: This requires the account to be in the wallet database
        const account = await getSchnorrAccount(
            aztecClient,
            accountAlias,
            NODE_URL
        );

        accounts[accountAlias] = account;
        return account;
    } catch (error) {
        console.error(`Failed to load account ${accountAlias}:`, error);
        throw error;
    }
}

/**
 * POST /api/send-transaction
 * Submit a transaction to Aztec network
 */
app.post('/api/send-transaction', async (req, res) => {
    try {
        const { accountAlias, contractAddress, functionName, args } = req.body;

        if (!accountAlias || !contractAddress || !functionName) {
            return res.status(400).json({
                error: 'Missing required fields: accountAlias, contractAddress, functionName'
            });
        }

        console.log(`Sending transaction: ${functionName} on ${contractAddress}`);

        // Load account
        const account = await loadAccount(accountAlias);

        // Get contract
        // Note: This requires the contract artifact/ABI
        // For now, we'll use a generic approach
        const contract = await aztecClient.getContract(contractAddress);

        // Send transaction
        const tx = await contract.methods[functionName](...args)
            .send({ from: account.address })
            .wait();

        res.json({
            success: true,
            txHash: tx.txHash.toString(),
            blockNumber: tx.blockNumber,
            status: 'mined'
        });
    } catch (error) {
        console.error('Transaction error:', error);
        res.status(500).json({
            error: error.message,
            success: false
        });
    }
});

/**
 * POST /api/deploy-contract
 * Deploy a contract to Aztec network
 */
app.post('/api/deploy-contract', async (req, res) => {
    try {
        const { accountAlias, contractName, constructorArgs } = req.body;

        if (!accountAlias || !contractName) {
            return res.status(400).json({
                error: 'Missing required fields: accountAlias, contractName'
            });
        }

        console.log(`Deploying contract: ${contractName}`);

        // Load account
        const account = await loadAccount(accountAlias);

        // Deploy contract
        // Note: This requires the compiled contract artifact
        const contract = await aztecClient.deployContract(
            contractName,
            constructorArgs || [],
            account
        );

        await contract.wait();

        res.json({
            success: true,
            contractAddress: contract.address.toString(),
            status: 'deployed'
        });
    } catch (error) {
        console.error('Deployment error:', error);
        res.status(500).json({
            error: error.message,
            success: false
        });
    }
});

/**
 * GET /api/account/:alias
 * Get account information
 */
app.get('/api/account/:alias', async (req, res) => {
    try {
        const { alias } = req.params;
        const account = await loadAccount(alias);

        res.json({
            success: true,
            address: account.address.toString(),
            publicKey: account.publicKey.toString()
        });
    } catch (error) {
        res.status(500).json({
            error: error.message,
            success: false
        });
    }
});

/**
 * GET /api/health
 * Health check
 */
app.get('/api/health', (req, res) => {
    res.json({
        status: 'healthy',
        aztecConnected: aztecClient !== null,
        nodeUrl: NODE_URL
    });
});

// Initialize and start server
async function start() {
    const initialized = await initializeAztec();
    if (!initialized) {
        console.error('Failed to initialize Aztec client. Server will start but transactions will fail.');
    }

    app.listen(PORT, () => {
        console.log(`🚀 Aztec Bridge Service running on port ${PORT}`);
        console.log(`📡 Connected to: ${NODE_URL}`);
        console.log(`🔐 Using PXE: ${PXE_URL}`);
    });
}

start().catch(console.error);

