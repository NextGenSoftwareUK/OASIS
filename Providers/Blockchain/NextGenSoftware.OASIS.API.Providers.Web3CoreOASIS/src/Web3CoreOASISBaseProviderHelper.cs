using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using Nethereum.JsonRpc.Client;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Nethereum.Contracts;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using System.Net.Http;
using Newtonsoft.Json;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Threading;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Nethereum.Util;
using Nethereum.Hex.HexTypes;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

internal static class Web3CoreOASISBaseProviderHelper
{
    public const string ProviderNotActivatedError = "Provider is not activated. Please activate provider and try again.";
    public const string InvalidAmountError = "Amount value must be greater than zero.";
    public const string Abi = @"
[
    {
      ""inputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""constructor""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""sender"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721IncorrectOwner"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""ERC721InsufficientApproval"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""approver"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidApprover"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidOperator"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidOwner"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""receiver"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidReceiver"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""sender"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidSender"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""ERC721NonexistentToken"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""OwnableInvalidOwner"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""account"",
          ""type"": ""address""
        }
      ],
      ""name"": ""OwnableUnauthorizedAccount"",
      ""type"": ""error""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""approved"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""Approval"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        },
        {
          ""indexed"": false,
          ""internalType"": ""bool"",
          ""name"": ""approved"",
          ""type"": ""bool""
        }
      ],
      ""name"": ""ApprovalForAll"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""previousOwner"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""newOwner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""OwnershipTransferred"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""Transfer"",
      ""type"": ""event""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""approve"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""balanceOf"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes32"",
          ""name"": ""avatarId"",
          ""type"": ""bytes32""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""createAvatar"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes32"",
          ""name"": ""avatarId"",
          ""type"": ""bytes32""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""createAvatarDetail"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes32"",
          ""name"": ""holonId"",
          ""type"": ""bytes32""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""createHolon"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""deleteAvatar"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""deleteAvatarDetail"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""deleteHolon"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getApproved"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": """",
          ""type"": ""address""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getAvatarById"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""uint256"",
              ""name"": ""EntityId"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""bytes32"",
              ""name"": ""ExternalId"",
              ""type"": ""bytes32""
            },
            {
              ""internalType"": ""bytes"",
              ""name"": ""Info"",
              ""type"": ""bytes""
            }
          ],
          ""internalType"": ""struct EntityOASIS"",
          ""name"": """",
          ""type"": ""tuple""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getAvatarDetailById"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""uint256"",
              ""name"": ""EntityId"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""bytes32"",
              ""name"": ""ExternalId"",
              ""type"": ""bytes32""
            },
            {
              ""internalType"": ""bytes"",
              ""name"": ""Info"",
              ""type"": ""bytes""
            }
          ],
          ""internalType"": ""struct EntityOASIS"",
          ""name"": """",
          ""type"": ""tuple""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""getAvatarDetailsCount"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""count"",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""getAvatarsCount"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""count"",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getHolonById"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""uint256"",
              ""name"": ""EntityId"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""bytes32"",
              ""name"": ""ExternalId"",
              ""type"": ""bytes32""
            },
            {
              ""internalType"": ""bytes"",
              ""name"": ""Info"",
              ""type"": ""bytes""
            }
          ],
          ""internalType"": ""struct EntityOASIS"",
          ""name"": """",
          ""type"": ""tuple""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""getHolonsCount"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""count"",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getTransferHistory"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""address"",
              ""name"": ""fromWalletAddress"",
              ""type"": ""address""
            },
            {
              ""internalType"": ""address"",
              ""name"": ""toWalletAddress"",
              ""type"": ""address""
            },
            {
              ""internalType"": ""string"",
              ""name"": ""fromProviderType"",
              ""type"": ""string""
            },
            {
              ""internalType"": ""string"",
              ""name"": ""toProviderType"",
              ""type"": ""string""
            },
            {
              ""internalType"": ""uint256"",
              ""name"": ""amount"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""string"",
              ""name"": ""memoText"",
              ""type"": ""string""
            }
          ],
          ""internalType"": ""struct NFTTransfer[]"",
          ""name"": """",
          ""type"": ""tuple[]""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        }
      ],
      ""name"": ""isApprovedForAll"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""metadataJson"",
          ""type"": ""string""
        }
      ],
      ""name"": ""mint"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""name"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""nextTokenId"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""nftMetadata"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": ""metadataJson"",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""nftTransfers"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""fromWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""toWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""fromProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""toProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""amount"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""memoText"",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""owner"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": """",
          ""type"": ""address""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""ownerOf"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": """",
          ""type"": ""address""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""renounceOwnership"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""safeTransferFrom"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""data"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""safeTransferFrom"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""fromWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""toWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""fromProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""toProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""amount"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""memoText"",
          ""type"": ""string""
        }
      ],
      ""name"": ""sendNFT"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""bool"",
          ""name"": ""approved"",
          ""type"": ""bool""
        }
      ],
      ""name"": ""setApprovalForAll"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""bytes4"",
          ""name"": ""interfaceId"",
          ""type"": ""bytes4""
        }
      ],
      ""name"": ""supportsInterface"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""symbol"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""tokenURI"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""transferFrom"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""newOwner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""transferOwnership"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""updateAvatar"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""updateAvatarDetail"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""updateHolon"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    }
  ]
";

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        public static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }
}
