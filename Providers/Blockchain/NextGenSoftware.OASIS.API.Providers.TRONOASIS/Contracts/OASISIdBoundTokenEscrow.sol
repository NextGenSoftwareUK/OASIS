// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import {OASISTokenEscrow} from "../../NextGenSoftware.OASIS.API.Providers.EthereumOASIS/Contracts/OASISTokenEscrow.sol";

/// @notice Exact-amount, single-use depositor-owned TRC20 deposits.
/// @dev Separate ABI/deployment from the existing aggregate/NFT escrow.
/// Reuses the canonical exact-transfer contract; no administrator or lock-all default.
/// TVM deployment and provider SDK selection must be verified before enabling this ABI.
contract OASISIdBoundTokenEscrow is OASISTokenEscrow {}
