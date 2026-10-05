# Smart Contract Generator (sc-gen)

## Overview

This project is a powerful .NET-based API service that simplifies the process of generating, compiling, and deploying smart
contracts for multiple blockchain platforms, including **Ethereum**, **Solana**, and **Radix**. It provides a unified interface to
work with each platform, handling the complexities of smart contract development behind the scenes.

## Supported Blockchains

- **Ethereum:** Generate, compile, and deploy Solidity-based smart contracts.
- **Solana:** Generate, compile, and deploy Rust-based smart contracts.
- **Radix:** Generate, compile, and deploy Scrypto-based smart contracts.

## Technology Stack

- **Backend Framework:** .NET 9.0, ASP.NET Core for the Web API.
- **Blockchain Interaction:**
    - **Ethereum:** `Nethereum.Web3`
    - **Solana & Radix:** Custom logic and external CLI tools.
- **Templating & AI:** `Handlebars.Net` and `Microsoft.SemanticKernel` for advanced templating and code generation.
- **API Documentation:** `Swashbuckle` and `Microsoft.AspNetCore.OpenApi` to generate Swagger/OpenAPI documentation.
- **Data Validation:** `FluentValidation` for robust request validation.
- **JSON Handling:** `Newtonsoft.Json` for serialization and deserialization.

## Prerequisites

Before you begin, ensure you have the following installed on your system:

- **.NET 9 SDK:** You can download it from the [official .NET website](https://dotnet.microsoft.com/download/dotnet/9.0).
- **An IDE or Code Editor:**
    - Visual Studio 2022
    - JetBrains Rider
    - Visual Studio Code with the C# Dev Kit extension

### Blockchain Development Tools

For local smart contract development and compilation, you will need to install the specific toolchain for each blockchain you
intend to work with.

#### Ethereum (Solidity)

- **Solidity Compiler (`solc`):** To compile your own Solidity contracts (`.sol` files) into the `.bin` and `.abi` formats that
  this API uses, you will need the `solc` compiler. It's recommended to use a version compatible with your contracts (e.g.,
  `0.8.x`).
    - Installation instructions can be found on
      the [official Solidity documentation](https://docs.soliditylang.org/en/latest/installing-solidity.html).

#### Solana (Rust)

- **Rust Toolchain:** The Solana ecosystem relies on Rust. Install the full toolchain, including `rustup` and `cargo`:
  ```bash
  curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
  ```
- **Solana CLI:** Install the Solana command-line tools, which are required for compiling and deploying Rust-based contracts.
  ```bash
  sh -c "$(curl -sSfL https://release.solana.com/stable/install)"
  ```

#### Radix (Scrypto)

- **Rust Toolchain:** Scrypto development also uses the Rust toolchain. Install it first:
  ```bash
  curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
  ```
- **Scrypto Toolchain:** This includes the `scrypto` CLI for building packages and `resim` for the Radix Engine Simulator. To
  install a specific version like `1.3.0` (as you requested), you would typically follow the official documentation's guide for
  version management. The standard installation is:
  ```bash
  cargo install scrypto-cli
  ```
  *Note: Always refer to the [official Radix documentation](https://docs.radixdlt.com/docs/scrypto-cli) for the latest recommended
  installation commands and versions.*

## How to Get Started

Follow these steps to get the project up and running on your local machine.

### 1. Clone the Repository

```bash
git clone https://github.com/QuantumStreet/SmartContractGen.git
cd sc-gen
```

### 2. Build the Solution

You can build the entire solution from the root directory:

```bash
dotnet build
```

### 3. Configure Your Settings

Review and update the `appsettings.json` file located in `src/SmartContractGen/ScGen.API/` to configure RPC endpoints and other
settings for the blockchains you intend to use.

### 4. Run the API

The main entry point for the application is the `ScGen.API` project. To run it, navigate to its directory and use the `dotnet run`
command:

```bash
cd src/SmartContractGen/ScGen.API
dotnet run
```

### 5. Access the API Documentation

Once the application is running, the API documentation (Swagger UI) will be available in your browser. By default, you can access
it at:

- **HTTP:** `http://localhost:5000/swagger`
- **HTTPS:** `https://localhost:5001/swagger`

From the Swagger UI, you can explore the available endpoints for generating, compiling, and deploying contracts on all supported
platforms.

## Project Structure

The solution is organized into the following main projects:

- `sc-gen.sln`: The main solution file.
- `src/SmartContractGen/ScGen.API/`: The ASP.NET Core Web API project.
- `src/SmartContractGen/ScGen.Lib/`: A class library containing the core logic, with specific implementations for each blockchain:
    - `ImplContracts/Ethereum/`
    - `ImplContracts/Solana/`
    - `ImplContracts/Radix/`
- `src/common/`: Shared code and building blocks.
- `tests/`: Placeholder for future tests.

SmartContractGen is a powerful .NET-based API service that simplifies the process of generating, compiling, and deploying smart
contracts for Ethereum and Solana blockchains. It provides a unified interface to work with both platforms, handling the
complexities of smart contract development behind the scenes.

## Features

- **Smart Contract Generation**: Generate Solidity (Ethereum) and Rust (Solana) smart contracts from JSON specifications
- **Contract Compilation**: Compile smart contracts into deployable bytecode
- **Deployment**: Deploy smart contracts to Ethereum and Solana networks
- **REST API**: Easy-to-use HTTP endpoints for all operations
- **Support for Multiple Blockchains**: Currently supports Ethereum and Solana

## Running

1. Clone the repository:
    ```bash 
   git clone https://github.com/QuantumStreet/SmartContractGen.git
    ```
2. Navigate to the API project directory:

   ```bash
   cd SmartContractGen/src/SmartContractGen/ScGen.API
   ```

3. Restore dependencies:
   ```bash
   dotnet restore
   ```

4. Build the solution:
   ```bash
   dotnet build
   ```

5. Configure the application by updating `appsettings.json` with your network settings.

## Configuration

Update the `appsettings.json` file with your configuration:

```json
{
  "Ethereum": {
    "RpcUrl": "http://127.0.0.1:8545",
    "PrivateKey": "YOUR_PRIVATE_KEY",
    "GasLimit": 3000000
  },
  "Solana": {
    "RpcUrl": "http://localhost:8899",
    "KeyPairPath": "/path/to/your/solana/keypair.json",
    "UseLocalValidator": true,
    "Pubkey": "YOUR_PUBLIC_KEY"
  },
  "Radix": {
    "UseResim": true,
    "Profile": "default",
    "AccountAddress": "YOUR_RADIX_ACCOUNT_ADDRESS",
    "AutoMintXrd": true
  }
}
```

## API Endpoints

### Generate Contract

**POST** `/api/v1/contracts/generate`

Generate a smart contract from a JSON specification.

**Request:**

- `language`: Blockchain platform (`Ethereum`, `Solana`, or `Radix`)
- `jsonFile`: JSON file containing contract specification

**Response:**

- Returns the generated smart contract source code

### Compile Contract

**POST** `/api/v1/contracts/compile`

Compile a smart contract.

**Request:**

- `language`: Blockchain platform  (`Ethereum`, `Solana`, or `Radix`)
- `source`: Source code file to compile

**Response:**

- Returns a ZIP file containing the compiled contract and ABI

### Deploy Contract

**POST** `/api/v1/contracts/deploy`

Deploy a compiled smart contract to the blockchain.

**Request:**

- `language`: Blockchain platform  (`Ethereum`, `Solana`, or `Radix`)
- `abiFile`: ABI file of the contract
- `compiledContractFile`: Compiled contract file

**Response:**

- Returns the transaction hash and contract address

## Usage Examples

Below are examples of how to use the API with `curl` for different blockchains.

### Generate a Contract

```bash
# For Ethereum, Solana, or Radix
# The language parameter determines the template to use.
curl -X POST "http://localhost:5000/api/v1/contracts/generate" \
  -F 'Language=Scrypto' \
  -F 'JsonFile=@/path/to/your/data.json'
```

### Compile a Contract

```bash
# For Ethereum (Solidity)
curl -X POST "http://localhost:5000/api/v1/contracts/compile" \
  -F 'Language=Ethereum' \
  -F 'Source=@/path/to/your/contract.sol'

# For Solana (Rust)
# Assumes you are uploading a zipped Rust project
curl -X POST "http://localhost:5000/api/v1/contracts/compile" \
  -F 'Language=Rust' \
  -F 'Source=@/path/to/your/solana-project.zip'

# For Radix (Scrypto)
# Assumes you are uploading a zipped Scrypto project
curl -X POST "http://localhost:5000/api/v1/contracts/compile" \
  -F 'Language=Scrypto' \
  -F 'Source=@/path/to/your/scrypto-project.zip'
```

### Deploy a Contract

```bash
# For Ethereum
curl -X POST "http://localhost:5000/api/v1/contracts/deploy" \
  -F 'Language=Ethereum' \
  -F 'Schema=@/path/to/contract.abi' \
  -F 'CompiledContractFile=@/path/to/contract.bin'

# For Solana
curl -X POST "http://localhost:5000/api/v1/contracts/deploy" \
  -F 'Language=Rust' \
  -F 'CompiledContractFile=@/path/to/your/program.so'

# For Radix
curl -X POST "http://localhost:5000/api/v1/contracts/deploy" \
  -F 'Language=Scrypto' \
  -F 'CompiledContractFile=@/path/to/your/package.wasm'
```

## Running the Application

To start the API server:

```bash
dotnet run --project src/SmartContractGen/ScGen.API
```

## Notes

- For production use, ensure you properly secure your private keys and API endpoints
- The default configuration uses local development networks. Update the RPC URLs for testnet or mainnet usage
- Make sure to have sufficient funds in your wallet for contract deployment

### Solana IDL Upload Feature

**New Feature**: The API now automatically attempts to upload the IDL (Interface Definition Language) after deploying Solana programs. The IDL upload process:

- **Automatically detects** IDL files in the `target/idl` directory after compilation
- **Attempts to upload** the IDL using `anchor idl init` after successful program deployment
- **Non-blocking**: If IDL upload fails, the deployment still succeeds (with a warning logged)
- **Fallback**: If IDL upload fails, clients must use local IDL files

#### Manual IDL Upload Steps

1. **Locate the generated IDL file** in your compiled project:
   ```bash
   # IDL is generated during compilation in:
   target/idl/YOUR_PROGRAM_NAME.json
   ```

2. **Upload the IDL to the deployed program**:
   ```bash
   # Navigate to your project directory
   cd /path/to/your/solana/project
   
   # Upload IDL to the deployed program
   anchor idl init --filepath target/idl/YOUR_PROGRAM_NAME.json --provider.cluster devnet YOUR_PROGRAM_ID
   ```

3. **Verify IDL upload**:
   ```bash
   # Check if IDL was uploaded successfully
   anchor idl fetch YOUR_PROGRAM_ID
   ```

#### Why IDL Upload is Required

- **Client Integration**: Enables `Program.fetchIdl()` to work from deployed programs
- **Type Safety**: Provides compile-time validation for TypeScript/JavaScript clients
- **Developer Experience**: Enables autocomplete and error checking in client applications
- **Prevents Errors**: Avoids "DeclaredProgramIdMismatch" errors in frontend applications

#### Example: UAT Factory Contract

```bash
# For our deployed UAT Factory contract
cd /Volumes/Storage/QS_Asset_Rail/contracts/uat-factory-final
anchor idl init --filepath target/idl/uat_factory.json --provider.cluster devnet 5sjHgtEMp6vzu3UhxBMMfcwRSc3mR2JoTJH8mA8JkiDH
```

                                                                  