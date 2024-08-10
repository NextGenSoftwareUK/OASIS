# Web3 Core OASIS

OASIS Provider `Web3CoreOASIS` is created to provide Avatar and Holon entities management, such as Save, Load, and Delete operations by using EVM-compatible blockchains.

Currently, OASIS supports the following EVM-compatible blockchains:

-   EthereumOASIS.
-   ArbitrumOASIS.
-   PolygonOASIS.
-   RootstockOASIS.

EthereumOASIS and ArbitrumOASIS are developed independently, with their own smart contracts and code base. PolygonOASIS and RootstockOASIS are based on Web3CoreOASIS, using its code base, new, efficient, and modern `Web3CoreOASIS` smart contract.

This approach helps develop a single code base that can be shared and used by multiple providers, making the development process faster.

## Web3CoreOASIS Smart Contract

Web3CoreOASIS Smart Contract comes with a single data entity, interface, and implementation.

### Data Entity

Data entity is an object that stores Avatar or Holon data efficiently.

```solidity
struct EntityOASIS {
    uint256 EntityId;
    bytes32 ExternalId;
    bytes Info;
}
```

-   `EntityId` - Unique integer-based identification value.
-   `ExternalId` - Unique `bytes32` based identification (GUID, can be represented as string) value of Avatar or Holon.
-   `Info` - JSON string containing Avatar and Holon data.

## Web3CoreOASIS

`Web3CoreOASIS` class is a Web3 client, used to call smart contract functions by making RPC calls to the chain.

## Web3CoreOASISBaseProvider

`Web3CoreOASISBaseProvider` class provides an implementation for `IOASISStorageProvider` interface, by using Web3CoreOASIS.

## Hardhat Usage

### Configuration

The Hardhat configuration is set up in `hardhat.config.cjs`, supporting local development, Rootstock, and Polygon networks. The configuration includes compiler settings, network endpoints, and necessary plugins.

To configure Hardhat and include support for the Polygon and Rootstock networks, follow these steps:

1. **Install Hardhat and Dependencies:**

    ```bash
    npm install --save-dev hardhat @nomicfoundation/hardhat-toolbox ethers
    ```

2. **Hardhat Configuration:**

    Create or update the `hardhat.config.cjs` file with the following content:

    ```javascript
    require("@nomicfoundation/hardhat-toolbox");

    module.exports = {
        solidity: "0.8.4",
        networks: {
            localhost: {
                url: "http://127.0.0.1:8545",
            },
            polygon: {
                url: "https://polygon-rpc.com/",
                accounts: [process.env.PRIVATE_KEY],
            },
            mumbai: {
                url: "https://rpc-mumbai.maticvigil.com",
                accounts: [process.env.PRIVATE_KEY],
            },
            rootstock: {
                url: "https://public-node.testnet.rsk.co",
                accounts: [process.env.PRIVATE_KEY],
            },
        },
    };
    ```

3. **Environment Variables:**

    Store your private key in a `.env` file:

    ```env
    PRIVATE_KEY=your_private_key
    ```

    Load environment variables in your `hardhat.config.cjs` file:

    ```javascript
    require("@nomicfoundation/hardhat-toolbox");
    require("dotenv").config();

    module.exports = {
        solidity: "0.8.4",
        networks: {
            localhost: {
                url: "http://127.0.0.1:8545",
            },
            polygon: {
                url: "https://polygon-rpc.com/",
                accounts: [process.env.PRIVATE_KEY],
            },
            mumbai: {
                url: "https://rpc-mumbai.maticvigil.com",
                accounts: [process.env.PRIVATE_KEY],
            },
            rootstock: {
                url: "https://public-node.testnet.rsk.co",
                accounts: [process.env.PRIVATE_KEY],
            },
        },
    };
    ```

#### Setting Up Private Keys and Addresses

To interact with the blockchain, you need a private key and an address. Here’s how you can get them:

1. **Generate a New Ethereum Address:**

    Use MetaMask or any other Ethereum wallet to generate a new address.

2. **Get Your Private Key:**

    Export your private key from MetaMask:

    - Open MetaMask and click on the account icon.
    - Select "Account Details" and then "Export Private Key".
    - Enter your password to reveal the private key.

3. **Store Your Private Key:**

    Save your private key in a `.env` file as shown above.

#### Faucets for Test Networks

To get test ETH or tokens on the test networks, use the following faucets:

-   **Polygon Mumbai Faucet:**
    [faucet.polygon.technology](https://faucet.polygon.technology/)

-   **Rootstock Faucet:**
    [faucet.rootstock.io](https://faucet.rootstock.io/)

#### Additional Resources

-   For a guide on porting Ethereum dApps to Rootstock, refer to [this tutorial](https://dev.rootstock.io/tutorials/ethereum-devs/port-ethereum-dapps/).

### Obtaining Etherscan API Key

To interact with Etherscan for verifying and publishing your smart contracts, you need to obtain an API key from Etherscan. Follow these steps to get your Etherscan API key:

1. **Sign Up on Etherscan:**

    - Visit [Etherscan](https://etherscan.io/) and sign up for a free account if you don't already have one.

2. **Login to Your Account:**

    - After signing up, log in to your Etherscan account.

3. **Access API Keys:**

    - Once logged in, navigate to the [API Keys](https://etherscan.io/myapikey) section from your account dashboard.

4. **Create a New API Key:**

    - Click on "Add" to create a new API key.
    - Provide a name for your API key (e.g., "Web3CoreOASIS") and click "Create New API Key Token".

5. **Copy Your API Key:**

    - After the key is generated, copy it for use in your Hardhat configuration.

6. **Store Your API Key Securely:**

    - Save the API key in a secure place. It’s recommended to store it in an environment variable in your `.env` file:

    ```env
    ETHERSCAN_API_KEY=your_etherscan_api_key
    ```

7. **Use Your API Key in Hardhat:**

    - Update your `hardhat.config.cjs` file to include the Etherscan API key for contract verification:

    ```javascript
    require("@nomicfoundation/hardhat-toolbox");
    require("dotenv").config();

    module.exports = {
        solidity: "0.8.4",
        networks: {
            localhost: {
                url: "http://127.0.0.1:8545",
            },
            polygon: {
                url: "https://polygon-rpc.com/",
                accounts: [process.env.PRIVATE_KEY],
            },
            mumbai: {
                url: "https://rpc-mumbai.maticvigil.com",
                accounts: [process.env.PRIVATE_KEY],
            },
            rootstock: {
                url: "https://public-node.testnet.rsk.co",
                accounts: [process.env.PRIVATE_KEY],
            },
        },
        etherscan: {
            apiKey: process.env.ETHERSCAN_API_KEY,
        },
    };
    ```

### Local Network

Start a local Hardhat node with:

```bash
npx hardhat node
```

### Deployment Script

Deploy the smart contract using:

```bash
npx hardhat run --network localhost scripts/deploy.js
```

### Testing

Run unit tests using:

```bash
npx hardhat test
```

## Code Style Rules

Code style rules for the project are enforced using `solhint`, which ensures consistent formatting and best practices for Solidity code. The standard styling rules include:

-   **Indentation:** Use 4 spaces for indentation.
-   **Quotes:** Use double quotes for string literals.
-   **Line Length:** Limit lines to 120 characters.
-   **Function Visibility:** Explicitly define visibility for all functions.
-   **Error Handling:** Use `require`, `assert`, and `revert` for error handling.
-   **Naming Conventions:** Follow camelCase for functions and variables, and PascalCase for contract names.

### Using Solhint

`solhint` is a linter for Solidity code that checks for style violations and potential issues. To use `solhint` in your project:

1. **Install Solhint:**

    ```bash
    npm install -D solhint
    ```

2. **Create a Solhint Configuration File:**

    Create a `.solhint.json` file in the root of your project with the following content:

    ```json
    {
        "extends": "solhint:recommended",
        "rules": {
            "indent": ["error", 4],
            "quotes": ["error", "double"],
            "max-line-length": ["error", 120],
            "visibility-modifier-order": ["error"],
            "func-visibility": ["error", { "ignoreConstructors": true }]
        }
    }
    ```

3. **Lint Your Solidity Code:**

    Run `solhint` on your Solidity files to check for style violations:

    ```bash
    npx solhint "contracts/**/*.sol"
    ```

These steps ensure your Solidity code adheres to the specified style guidelines, improving readability and maintainability.

## Deploy and Test

### Deployment

A bash script `deploy.sh` automates the deployment process. It accepts the network as an argument and optionally runs tests after deployment.

```bash
#!/bin/bash
# Usage: ./deploy.sh [network] [run_tests]

NETWORK=$1
RUN_TESTS=$2

if [ -z "$NETWORK" ]; then
  echo "Usage: ./deploy.sh [network] [run_tests]"
  exit 1
fi

if [ "$NETWORK" == "development" ]; then
  npx hardhat node &
  HARDHAT_PID=$!
  sleep 5
fi

npx hardhat run --network $NETWORK scripts/deploy.js

if [ "$RUN_TESTS" == "true" ]; then
  npx hardhat test --network $NETWORK
fi

if [ "$NETWORK" == "development" ]; then
  kill $HARDHAT_PID
fi
```

### Unit Tests

Unit tests are written using the Hardhat framework and Chai assertion library. They include tests for creating avatars, loading avatars, and ensuring data integrity.

```javascript
const { expect } = require("chai");
const { ethers } = require("hardhat");

describe("Web3CoreOASIS", function () {
    let contract;
    let owner;

    before(async () => {
        const Web3CoreOASIS = await ethers.getContractFactory("Web3CoreOASIS");
        [owner] = await ethers.getSigners();
        contract = await Web3CoreOASIS.deploy();
        await contract.deployed();
    });

    it("should create an avatar", async function () {
        const avatarId = ethers.utils.formatBytes32String("Avatar1");
        const info = ethers.utils.formatBytes32String("Info1");

        await contract.createAvatar(1, avatarId, info);
        const avatar = await contract.getAvatarById(1);

        expect(avatar.ExternalId).to.equal(avatarId);
    });

    it("should load an avatar", async function () {
        const avatar = await contract.getAvatarById(1);
        expect(avatar.ExternalId).to.equal(
            ethers.utils.formatBytes32String("Avatar1")
        );
    });

    it("should update an avatar", async function () {
        const newInfo = ethers.utils.formatBytes32String("NewInfo");
        await contract.updateAvatar(1, newInfo);
        const updatedAvatar = await contract.getAvatarById(1);
        expect(updatedAvatar.Info).to.equal(newInfo);
    });
});
```

### Performance Tests

Performance tests measure the execution time and gas usage of contract functions.

```javascript
describe("Performance Tests", function () {
    let contract;
    let owner;

    before(async () => {
        const Web3CoreOASIS = await ethers.getContractFactory("Web3CoreOASIS");
        [owner] = await ethers.getSigners();
        contract = await Web3CoreOASIS.deploy();
        await contract.deployed();
    });

    it("should measure performance of createAvatar", async function () {
        const avatarId = ethers.utils.formatBytes32String("Avatar1");
        const info = ethers.utils.formatBytes32String("Info1");

        const startTime = process.hrtime();
        await contract.createAvatar(1, avatarId, info);
        const endTime = process.hrtime(startTime);

        console.log(
            `Execution time for createAvatar: ${endTime[0]}s ${
                endTime[1] / 1000000
            }ms`
        );
    });
});
```

### Gas Estimate Tests

Gas estimate tests ensure that contract functions are optimized for gas usage.

```javascript
describe("Gas Estimate Tests", function () {
    let contract;
    let owner;

    before(async () => {
        const Web3CoreOASIS = await ethers.getContractFactory("Web3CoreOASIS");
        [owner] = await ethers.getSigners();
        contract = await Web3CoreOASIS.deploy();
        await contract.deployed();
    });

    it("should measure gas usage for createAvatar", async function () {
        const avatarId = ethers.utils.formatBytes32String("Avatar1");
        const info = ethers.utils.formatBytes32String("Info1");

        const tx = await contract.createAvatar(1, avatarId, info);
        const receipt = await tx.wait();

        console.log(`Gas used for createAvatar: ${receipt.gasUsed.toString()}`);
    });
});
```
