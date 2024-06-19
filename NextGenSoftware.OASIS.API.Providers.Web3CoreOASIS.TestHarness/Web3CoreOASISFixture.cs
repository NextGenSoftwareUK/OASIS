namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.TestHarness;

public sealed class Web3CoreOASISFixture
{
    private const string BlockchainUrl = "http://127.0.0.1:8545";
    private const string AccountPrivateKey = "0x4e964e893315ea9c669a774ab533e8c708e184d8f59f4a895e7a326f605296e9";
    private const string ContractAddress = "0x7BdEAEbF915D9BAdfe1D789FD4387CCacE9d2ab7";
    private const string Abi = @"
[
    {
      ""inputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""constructor""
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
    }
  ]
";

    public Web3CoreOASIS Web3CoreOASIS { get; private set; }

    public Web3CoreOASISFixture() => Web3CoreOASIS = new(AccountPrivateKey, BlockchainUrl, ContractAddress, Abi);
}
