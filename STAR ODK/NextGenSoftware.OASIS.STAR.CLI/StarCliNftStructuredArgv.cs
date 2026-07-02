using System;
using System.IO;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.STAR.CLI
{
    /// <summary>Structured argv for NFT / GeoNFT verbs in <c>--non-interactive</c> mode.</summary>
    internal static class StarCliNftStructuredArgv
    {
        /// <summary>How to run non-interactive <c>nft import</c> for a resolved file path.</summary>
        internal enum NftNonInteractiveImportKind
        {
            Web4OrOpaqueFile,
            Web3MintFromJson,
            Web3TokenFromJson,
        }

        public static bool TryGetArgsAfterVerb(string[] inputArgs, string verb, out string[] tail, out string errorMessage)
        {
            tail = null;
            errorMessage = null;
            if (inputArgs == null || inputArgs.Length == 0)
            {
                errorMessage = "Missing arguments.";
                return false;
            }

            int idx = -1;
            for (int i = 0; i < inputArgs.Length; i++)
            {
                if (string.Equals(inputArgs[i], verb, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
            {
                errorMessage = $"Expected subcommand '{verb}'.";
                return false;
            }

            int len = inputArgs.Length - (idx + 1);
            if (len <= 0)
            {
                errorMessage = $"'{verb}' requires additional arguments in non-interactive mode.";
                return false;
            }

            tail = new string[len];
            Array.Copy(inputArgs, idx + 1, tail, 0, len);
            return true;
        }

        public static bool TryGetMintRequestJsonPath(string[] inputArgs, out string path, out string errorMessage)
        {
            path = null;
            if (!TryGetArgsAfterVerb(inputArgs, "mint", out string[] tail, out errorMessage))
                return false;
            path = tail[0]?.Trim();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                errorMessage = "mint requires an existing JSON file path (IMintWeb4NFTRequest / MintWeb4NFTRequest).";
                return false;
            }

            return true;
        }

        public static bool TryGetBurnRequestJsonPath(string[] inputArgs, out string path, out string errorMessage)
        {
            path = null;
            if (!TryGetArgsAfterVerb(inputArgs, "burn", out string[] tail, out errorMessage))
                return false;
            path = tail[0]?.Trim();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                errorMessage = "burn requires an existing JSON file path (BurnWeb3NFTRequest).";
                return false;
            }

            return true;
        }

        public static bool TryGetRemintTargetId(string[] inputArgs, out string idOrName, out string errorMessage)
        {
            idOrName = null;
            if (!TryGetArgsAfterVerb(inputArgs, "remint", out string[] tail, out errorMessage))
                return false;
            idOrName = tail[0]?.Trim();
            if (string.IsNullOrWhiteSpace(idOrName))
            {
                errorMessage = "remint requires a WEB4 NFT / GeoNFT id (GUID) or name.";
                return false;
            }

            return true;
        }

        public static bool TryGetSendArgs(string[] inputArgs, out string from, out string to, out string token, out string memo, out string errorMessage)
        {
            from = to = token = memo = null;
            if (!TryGetArgsAfterVerb(inputArgs, "send", out string[] tail, out errorMessage))
                return false;
            if (tail.Length < 4)
            {
                errorMessage = "send requires: <fromWallet> <toWallet> <tokenAddress> <memoText>";
                return false;
            }

            from = tail[0]?.Trim();
            to = tail[1]?.Trim();
            token = tail[2]?.Trim();
            memo = tail[3]?.Trim();
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(token))
            {
                errorMessage = "send: from, to, and tokenAddress must be non-empty.";
                return false;
            }

            memo ??= "";
            return true;
        }

        public static bool TryGetPlaceGeoJsonPath(string[] inputArgs, out string path, out string errorMessage)
        {
            path = null;
            if (!TryGetArgsAfterVerb(inputArgs, "place", out string[] tail, out errorMessage))
                return false;
            path = tail[0]?.Trim();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                errorMessage = "place requires an existing JSON file path (PlaceWeb4GeoSpatialNFTRequest).";
                return false;
            }

            return true;
        }

        public static bool TryGetExportDest(string[] inputArgs, out string nftIdOrName, out string filePath, out string errorMessage)
        {
            nftIdOrName = filePath = null;
            if (!TryGetArgsAfterVerb(inputArgs, "export", out string[] tail, out errorMessage))
                return false;
            if (tail.Length < 2)
            {
                errorMessage = "export requires: <web4NftOrGeoIdOrName> <destinationFilePath>";
                return false;
            }

            nftIdOrName = tail[0]?.Trim();
            filePath = tail[1]?.Trim();
            if (string.IsNullOrWhiteSpace(nftIdOrName) || string.IsNullOrWhiteSpace(filePath))
            {
                errorMessage = "export: id and destination path must be non-empty.";
                return false;
            }

            return true;
        }

        public static bool TryGetImportPath(string[] inputArgs, out string path, out string errorMessage)
        {
            path = null;
            if (!TryGetArgsAfterVerb(inputArgs, "import", out string[] tail, out errorMessage))
                return false;
            path = tail[0]?.Trim();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                errorMessage = "import requires an existing file path (see interactive import flow for format).";
                return false;
            }

            return true;
        }

        /// <summary><c>import web3-mint &lt;MintWeb4NFTRequest.json&gt;</c> (non-interactive <c>nft</c> only).</summary>
        public static bool TryGetImportWeb3MintJsonPath(string[] inputArgs, out string path, out string errorMessage)
        {
            path = null;
            errorMessage = null;
            if (!TryGetArgsAfterVerb(inputArgs, "import", out string[] tail, out errorMessage))
                return false;
            if (tail.Length == 0 || !string.Equals(tail[0], "web3-mint", StringComparison.OrdinalIgnoreCase))
                return false;
            if (tail.Length < 2)
            {
                errorMessage = "import web3-mint requires a path to MintWeb4NFTRequest JSON.";
                return false;
            }

            path = tail[1]?.Trim();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                errorMessage = "import web3-mint requires an existing JSON file path.";
                return false;
            }

            return true;
        }

        /// <summary><c>import web3-token &lt;ImportWeb3NFTRequest.json&gt;</c> (non-interactive <c>nft</c> only).</summary>
        public static bool TryGetImportWeb3TokenJsonPath(string[] inputArgs, out string path, out string errorMessage)
        {
            path = null;
            errorMessage = null;
            if (!TryGetArgsAfterVerb(inputArgs, "import", out string[] tail, out errorMessage))
                return false;
            if (tail.Length == 0 || !string.Equals(tail[0], "web3-token", StringComparison.OrdinalIgnoreCase))
                return false;
            if (tail.Length < 2)
            {
                errorMessage = "import web3-token requires a path to ImportWeb3NFTRequest JSON.";
                return false;
            }

            path = tail[1]?.Trim();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                errorMessage = "import web3-token requires an existing JSON file path.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Non-interactive <c>nft import</c>: primary form <c>import &lt;file&gt;</c> (JSON shape selects WEB3 mint vs token vs WEB4 file).
        /// Aliases: <c>import web3-mint &lt;file&gt;</c>, <c>import web3-token &lt;file&gt;</c>.
        /// </summary>
        public static bool TryResolveNftNonInteractiveImport(string[] inputArgs, out string path, out NftNonInteractiveImportKind kind, out string errorMessage)
        {
            path = null;
            kind = NftNonInteractiveImportKind.Web4OrOpaqueFile;
            errorMessage = null;
            if (!TryGetArgsAfterVerb(inputArgs, "import", out string[] tail, out errorMessage))
                return false;

            if (tail.Length >= 2 && string.Equals(tail[0], "web3-mint", StringComparison.OrdinalIgnoreCase))
            {
                path = tail[1]?.Trim();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    errorMessage = "import web3-mint requires an existing JSON file path.";
                    return false;
                }

                kind = NftNonInteractiveImportKind.Web3MintFromJson;
                return true;
            }

            if (tail.Length >= 2 && string.Equals(tail[0], "web3-token", StringComparison.OrdinalIgnoreCase))
            {
                path = tail[1]?.Trim();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    errorMessage = "import web3-token requires an existing JSON file path.";
                    return false;
                }

                kind = NftNonInteractiveImportKind.Web3TokenFromJson;
                return true;
            }

            if (tail.Length != 1)
            {
                errorMessage = "nft import requires a single file path (JSON is auto-detected), or legacy: import web3-mint <file> | import web3-token <file>.";
                return false;
            }

            path = tail[0]?.Trim();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                errorMessage = "import requires an existing file path.";
                return false;
            }

            kind = ClassifyNftImportFile(path);
            return true;
        }

        private static NftNonInteractiveImportKind ClassifyNftImportFile(string filePath)
        {
            try
            {
                string text = File.ReadAllText(filePath);
                MintWeb4NFTRequest mintTry = JsonConvert.DeserializeObject<MintWeb4NFTRequest>(text);
                if (mintTry?.Web3NFTs != null && mintTry.Web3NFTs.Count > 0)
                    return NftNonInteractiveImportKind.Web3MintFromJson;

                ImportWeb3NFTRequest tokTry = JsonConvert.DeserializeObject<ImportWeb3NFTRequest>(text);
                if (tokTry != null && !string.IsNullOrWhiteSpace(tokTry.NFTTokenAddress))
                    return NftNonInteractiveImportKind.Web3TokenFromJson;
            }
            catch
            {
                // Not matching mint/token JSON — fall through to WEB4 / opaque file import.
            }

            return NftNonInteractiveImportKind.Web4OrOpaqueFile;
        }

        /// <summary>First argv token after <paramref name="verb"/> (e.g. clone / convert source id).</summary>
        public static bool TryGetFirstTokenAfterVerb(string[] inputArgs, string verb, out string token, out string errorMessage)
        {
            token = null;
            if (!TryGetArgsAfterVerb(inputArgs, verb, out string[] tail, out errorMessage))
                return false;
            if (tail == null || tail.Length < 1 || string.IsNullOrWhiteSpace(tail[0]))
            {
                errorMessage = $"'{verb}' requires a non-empty id or name in non-interactive mode.";
                return false;
            }

            token = tail[0].Trim();
            return true;
        }
    }
}
