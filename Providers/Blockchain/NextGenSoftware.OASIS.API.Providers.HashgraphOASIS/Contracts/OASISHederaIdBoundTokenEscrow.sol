// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import {OASISTokenEscrow} from "../../NextGenSoftware.OASIS.API.Providers.EthereumOASIS/Contracts/OASISTokenEscrow.sol";

interface IHtsDepositAssociation {
    function isAssociated() external view returns (bool);
    function associate() external returns (uint256);
}

/// @notice Exact-amount HTS deposits sharing the canonical per-deposit custody rules.
/// @dev Separate ABI/deployment. HTS association is explicit, before approval/deposit.
/// Native HTS int64 bounds, runtime verification and fees must also be enforced by the SDK.
contract OASISHederaIdBoundTokenEscrow is OASISTokenEscrow {
    function isTokenAssociated(address tokenAddress) external view returns (bool) {
        require(tokenAddress != address(this) && tokenAddress.code.length > 0, "Invalid token");
        return IHtsDepositAssociation(tokenAddress).isAssociated();
    }

    function associateToken(address tokenAddress) external nonReentrant {
        require(tokenAddress != address(this) && tokenAddress.code.length > 0, "Invalid token");
        IHtsDepositAssociation token = IHtsDepositAssociation(tokenAddress);
        require(!token.isAssociated(), "Already associated");
        require(token.associate() == 22, "HTS association failed");
        require(token.isAssociated(), "HTS association readback failed");
    }
}
