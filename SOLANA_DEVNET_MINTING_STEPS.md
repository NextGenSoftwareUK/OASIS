# Solana Devnet NFT Minting Playbook

This check list documents everything needed to mint NFTs against the Web4/OASIS stack on Solana **devnet**. Follow it end-to-end whenever you reset the environment.

---

## 1. Generate + Fund the Devnet Keypair

```bash
solana-keygen new --outfile solana-devnet.json --no-passphrase --force
solana config set --url https://api.devnet.solana.com
```

Use the JSON-RPC faucet (the CLI faucet is rate limited and often returns 400s):

```bash
curl -s -X POST -H 'Content-Type: application/json' \
  --data '{"jsonrpc":"2.0","id":1,"method":"requestAirdrop","params":["<DEVNET_PUBLIC_KEY>",1000000000]}' \
  https://api.devnet.solana.com
```

Verify balance at least once:

```bash
curl -s -X POST -H 'Content-Type: application/json' \
  --data '{"jsonrpc":"2.0","id":1,"method":"getBalance","params":["<DEVNET_PUBLIC_KEY>"]}' \
  https://api.devnet.solana.com
```

## 2. Convert the Secret Key to Base58 (Solana format)

```bash
python3 - <<'PY'
import json
from pathlib import Path
alphabet = '123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz'
def base58_encode(data: bytes) -> str:
    num = int.from_bytes(data, 'big')
    enc = ''
    while num:
        num, rem = divmod(num, 58)
        enc = alphabet[rem] + enc
    prefix = '1' * len(data) - len(data.lstrip(b'\0'))
    return prefix + (enc or '1')
arr = json.loads(Path('solana-devnet.json').read_text())
print(base58_encode(bytes(arr)))
PY
```

Record:

- `WalletMnemonicWords`: the seed phrase printed by `solana-keygen`
- `PrivateKey`: the base58 string from the script above
- `PublicKey`: the devnet address (output of `solana address`)  

## 3. Update Both DNA Configs

There are **two** DNA files. Both must reference the same devnet credentials.

1. `OASIS_DNA.json`
2. `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`

Replace the `SolanaOASIS` block with something like:

```json
"SolanaOASIS": {
  "WalletMnemonicWords": "<seed phrase>",
  "PrivateKey": "<base58 secret>",
  "PublicKey": "<devnet public key>",
  "ConnectionString": "https://api.devnet.solana.com"
}
```

Verify that the WebAPI copy isn’t minified incorrectly after edits.

## 4. Restart the API Cleanly

```bash
# stop every running webapi instance
pgrep -f "dotnet run" | xargs kill

# start from the project directory (not repo root)
cd ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

Once the console shows `Now listening on http://localhost:5003` you’re good.

## 5. Register + Activate Solana

```bash
TOKEN=$(curl -sk -X POST https://localhost:5004/api/avatar/authenticate \
  -H 'Content-Type: application/json' \
  -d '{"username":"metabricks_admin","password":"Uppermall1!"}' | jq -r '.result.result.jwtToken')

curl -sk -X POST https://localhost:5004/api/provider/register-provider-type/SolanaOASIS \
  -H "Authorization: Bearer $TOKEN"

curl -sk -X POST https://localhost:5004/api/provider/activate-provider/SolanaOASIS \
  -H "Authorization: Bearer $TOKEN"
```

## 6. Mint the NFT

```bash
curl -sk -X POST https://localhost:5004/api/nft/mint-nft \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
        "Title": "MetaBrick Test NFT",
        "Description": "Test NFT minted via devnet.oasisweb4.one",
        "Symbol": "MBRICK",
        "OnChainProvider": "SolanaOASIS",
        "OffChainProvider": "MongoDBOASIS",
        "NFTOffChainMetaType": "ExternalJSONURL",
        "NFTStandardType": "SPL",
        "JSONMetaDataURL": "https://gateway.pinata.cloud/ipfs/Qmag8SxBHha1K6zvxqqYANjVza1HmPbSwempw2LpFW6X88",
        "ImageUrl": "https://gateway.pinata.cloud/ipfs/bafkreibhok44eomzkubmt3e2kzxip3w3b4pclixvgff5q7awhfa7kwlwsq",
        "ThumbnailUrl": "https://gateway.pinata.cloud/ipfs/bafkreibhok44eomzkubmt3e2kzxip3w3b4pclixvgff5q7awhfa7kwlwsq",
        "Price": 0.02,
        "NumberToMint": 1,
        "StoreNFTMetaDataOnChain": false,
        "SendToAddressAfterMinting": "85ArqfA2fy8spGcMGsSW7cbEJAWj26vewmmoG2bwkgT9"
      }'
```

Successful output includes:

- `mintTransactionHash`
- `nftTokenAddress`
- `SendNFTTransactionHash`
- `oasisMintWalletAddress` (should equal the devnet public key we configured)

## 7. Troubleshooting Tips

- If you get `Invalid base58 data`, check that the WebAPI is using the new DNA by verifying `OwnerPublicKey` in the error message.
- Ensure only **one** `dotnet run` process is active—multiple listeners cause odd behaviour.
- Use https://explorer.solana.com/?cluster=devnet with the mint hash or token address to double-check on-chain state.

---

_Last verified: 2025-11-09 with mint hash `4cSaJrbW4uqXvvoqkqmBy28jxrrGUeMW9ZqRaeik4wva8d4xSGYo1mLNizMpoYnm6MQ6LABtSNryGoQ5oDvBbd9K`._
