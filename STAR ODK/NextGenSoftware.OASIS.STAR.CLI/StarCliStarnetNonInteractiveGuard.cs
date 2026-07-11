using System;

namespace NextGenSoftware.OASIS.STAR.CLI
{
    /// <summary>
    /// Central checks so <c>--non-interactive</c> never falls through into STARNET UI wizards
    /// when required ids or tokens are missing.
    /// </summary>
    internal static class StarCliStarnetNonInteractiveGuard
    {
        /// <summary>Subcommands that are menu/wizard-only at the CLI (no argv surface yet).</summary>
        public static bool IsWizardOnlySubcommand(string subCommandParamLower)
        {
            if (string.IsNullOrEmpty(subCommandParamLower))
                return false;

            switch (subCommandParamLower)
            {
                default:
                    return false;
            }
        }

        /// <summary>
        /// When non-interactive, validates argv-derived identity tokens before predicates run.
        /// </summary>
        /// <returns>true if an error was written and the caller should return.</returns>
        public static bool WriteHolonSubCommandViolationIfNeeded(
            bool jsonMode,
            string holonDisplayName,
            string verbLower,
            string id,
            string[] inputArgs,
            string subCommandParam3,
            string subCommandParam4,
            bool web3,
            bool web4,
            bool hasMintPredicate,
            bool hasBurnPredicate,
            bool hasClonePredicate,
            bool hasConvertPredicate,
            bool hasImportPredicate,
            bool hasExportPredicate,
            bool hasAddWeb4ToCollectionPredicate,
            bool hasRemoveWeb4FromCollectionPredicate,
            bool hasAddDependencyPredicate,
            bool hasRemoveDependencyPredicate)
        {
            if (string.IsNullOrEmpty(verbLower))
                return false;

            string entity = string.IsNullOrEmpty(holonDisplayName) ? "holon" : holonDisplayName;

            switch (verbLower)
            {
                case "list":
                case "search":
                case "create":
                case "light":
                    return false;

                case "mint":
                case "burn":
                case "clone":
                case "convert":
                case "import":
                case "export":
                case "remint":
                case "place":
                case "send":
                    // Structured argv is validated in Program / Lib before wizards run.
                    return false;
            }

            if (!TryGetRequiredPrimaryHolonId(verbLower, id, inputArgs, subCommandParam3, web3, web4, out string effectiveId))
                return false;

            if (string.IsNullOrWhiteSpace(effectiveId))
            {
                StarCliShellOutput.WriteError(jsonMode, 2,
                    $"Non-interactive '{entity} {verbLower}' requires a non-empty id or name token.",
                    ExampleLineFor(holonDisplayName, verbLower, web3, web4));
                return true;
            }

            if (verbLower == "add" && hasAddWeb4ToCollectionPredicate)
            {
                if (string.IsNullOrWhiteSpace(subCommandParam3))
                {
                    StarCliShellOutput.WriteError(jsonMode, 2,
                        $"Non-interactive '{entity} add' requires collection id and NFT id (or name) tokens.",
                        ExampleLineFor(holonDisplayName, "add", web3, web4));
                    return true;
                }
            }

            if (verbLower == "remove" && hasRemoveWeb4FromCollectionPredicate)
            {
                if (string.IsNullOrWhiteSpace(subCommandParam3))
                {
                    StarCliShellOutput.WriteError(jsonMode, 2,
                        $"Non-interactive '{entity} remove' requires collection id and NFT id (or name) tokens.",
                        ExampleLineFor(holonDisplayName, "remove", web3, web4));
                    return true;
                }
            }

            if (verbLower == "adddependency" && hasAddDependencyPredicate)
            {
                if (string.IsNullOrWhiteSpace(subCommandParam3) || string.IsNullOrWhiteSpace(subCommandParam4))
                {
                    StarCliShellOutput.WriteError(jsonMode, 2,
                        $"Non-interactive '{entity} adddependency' requires parent id, dependency id, and version tokens.",
                        "Example: star --non-interactive oapp adddependency <parentId> <dependencyId> <version>");
                    return true;
                }
            }

            if (verbLower == "removedependency" && hasRemoveDependencyPredicate)
            {
                if (string.IsNullOrWhiteSpace(subCommandParam3) || string.IsNullOrWhiteSpace(subCommandParam4))
                {
                    StarCliShellOutput.WriteError(jsonMode, 2,
                        $"Non-interactive '{entity} removedependency' requires parent id, dependency id, and version tokens.",
                        "Example: star --non-interactive oapp removedependency <parentId> <dependencyId> <version>");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// false = this verb is not subject to primary holon id validation here (handled elsewhere or N/A).
        /// </summary>
        private static bool TryGetRequiredPrimaryHolonId(
            string verbLower,
            string id,
            string[] inputArgs,
            string subCommandParam3,
            bool web3,
            bool web4,
            out string effectiveId)
        {
            effectiveId = string.Empty;
            switch (verbLower)
            {
                case "update":
                case "delete":
                    if (web3 || web4)
                        effectiveId = inputArgs != null && inputArgs.Length > 3 ? inputArgs[3] : string.Empty;
                    else
                        effectiveId = id ?? string.Empty;
                    return true;

                case "show":
                    if (web3 || web4)
                        effectiveId = subCommandParam3 ?? string.Empty;
                    else if (string.Equals(id, "detailed", StringComparison.OrdinalIgnoreCase))
                    {
                        effectiveId = inputArgs != null && inputArgs.Length > 3
                            ? inputArgs[3] ?? string.Empty
                            : string.Empty;
                    }
                    else
                        effectiveId = id ?? string.Empty;
                    return true;

                case "download":
                case "install":
                case "uninstall":
                case "publish":
                case "unpublish":
                case "republish":
                case "activate":
                case "deactivate":
                case "add":
                case "remove":
                case "adddependency":
                case "removedependency":
                    effectiveId = id ?? string.Empty;
                    return true;

                default:
                    return false;
            }
        }

        private static string ExampleLineFor(string holonDisplayName, string verb, bool web3, bool web4)
        {
            string root = RootCommandForEntity(holonDisplayName);
            if (verb == "show" && (web3 || web4))
                return $"Example: star --non-interactive {root} show web4 <idOrName>";
            string web = (web3 || web4) ? " web4 " : " ";
            return $"Example: star --non-interactive {root} {verb}{web}<idOrName>";
        }

        private static string RootCommandForEntity(string holonDisplayName)
        {
            if (string.IsNullOrEmpty(holonDisplayName))
                return "oapp";

            string u = holonDisplayName.ToUpperInvariant();
            if (u.Contains("OAPP") && u.Contains("TEMPLATE"))
                return "oapp template";
            if (u.Contains("NFT") && u.Contains("COLLECTION"))
                return "nft collection";
            if (u == "NFT" || u.Contains("NFT'S"))
                return "nft";
            if (u.Contains("GEO") && u.Contains("NFT") && u.Contains("COLLECTION"))
                return "geonft collection";
            if (u.Contains("GEO") && u.Contains("NFT"))
                return "geonft";
            if (u == "OAPP")
                return "oapp";
            if (u == "HAPP")
                return "happ";

            return holonDisplayName.ToLowerInvariant().Replace(' ', '-');
        }
    }
}
