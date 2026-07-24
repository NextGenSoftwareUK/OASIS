using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.STAR.CLI
{
    /// <summary>
    /// Parses global STAR CLI flags (any position). Remaining tokens are the command pipeline.
    /// </summary>
    public sealed class StarCliInvocation
    {
        private static readonly HashSet<string> CommandsThatSkipBeamIn = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "help", "version", "status", "dna", "ignite", "extinguish", "exit"
        };

        /// <summary>Commands that do not require an avatar session (after <c>avatar beamin</c> prefix is stripped). Used for both interactive and <c>--non-interactive</c> boot.</summary>
        public static bool CommandSkipsAvatarBeamIn(string firstVerb) =>
            firstVerb != null && CommandsThatSkipBeamIn.Contains(firstVerb);

        public bool NonInteractive { get; private set; }
        public bool JsonOutput { get; private set; }
        public bool Quiet { get; private set; }
        public bool AssumeYes { get; private set; }
        /// <summary>Optional credentials from <c>--username</c> / <c>--password</c> (avoid for production; prefer env vars).</summary>
        public string BeamInUsername { get; private set; }
        public string BeamInPassword { get; private set; }
        /// <summary>Optional cap only: when &gt; 0, limits STARNET search rows and ambiguous find hints (mirrors CLIEngine.MaxHolonSearchResults). Omitted or 0 leaves defaults unlimited.</summary>
        public int MaxHolonSearchResults { get; private set; }
        public IReadOnlyList<string> CommandArgs { get; private set; }

        private StarCliInvocation() { }

        /// <summary>
        /// Strips a leading <c>avatar beamin &lt;user&gt; &lt;pass&gt;</c> sequence (if present) and merges env/flag credentials.
        /// </summary>
        public string[] GetCommandArgsAfterOptionalAvatarBeamIn(out string resolvedUsername, out string resolvedPassword)
        {
            resolvedUsername = BeamInUsername ?? Environment.GetEnvironmentVariable("STAR_CLI_USERNAME");
            resolvedPassword = BeamInPassword ?? Environment.GetEnvironmentVariable("STAR_CLI_PASSWORD");

            if (CommandArgs.Count >= 4
                && string.Equals(CommandArgs[0], "avatar", StringComparison.OrdinalIgnoreCase)
                && string.Equals(CommandArgs[1], "beamin", StringComparison.OrdinalIgnoreCase))
            {
                resolvedUsername = CommandArgs[2];
                resolvedPassword = CommandArgs[3];
                if (CommandArgs.Count == 4)
                    return Array.Empty<string>();
                return CommandArgs.Skip(4).ToArray();
            }

            return CommandArgs.Count == 0 ? Array.Empty<string>() : CommandArgs.ToArray();
        }

        public static StarCliInvocation Parse(string[] args)
        {
            if (args == null)
                args = Array.Empty<string>();

            bool ni = false, json = false, quiet = false, yes = false;
            string beamUser = null, beamPass = null;
            int maxHolonSearch = 0;
            var cmd = new List<string>(args.Length);

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a == null)
                    continue;

                switch (a)
                {
                    case "--non-interactive":
                    case "-n":
                        ni = true;
                        break;
                    case "--json":
                        json = true;
                        break;
                    case "--quiet":
                    case "-q":
                        quiet = true;
                        break;
                    case "--yes":
                    case "-y":
                        yes = true;
                        break;
                    case "--search-limit":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int lim) && lim > 0)
                        {
                            maxHolonSearch = lim;
                            i++;
                        }
                        break;
                    case "--username":
                        if (i + 1 < args.Length)
                        {
                            beamUser = args[++i];
                        }
                        break;
                    case "--password":
                        if (i + 1 < args.Length)
                        {
                            beamPass = args[++i];
                        }
                        break;
                    default:
                        cmd.Add(a);
                        break;
                }
            }

            // Quieter startup for automation / machine-readable runs
            if (ni || json)
                quiet = true;

            return new StarCliInvocation
            {
                NonInteractive = ni,
                JsonOutput = json,
                Quiet = quiet,
                AssumeYes = yes,
                BeamInUsername = beamUser,
                BeamInPassword = beamPass,
                MaxHolonSearchResults = maxHolonSearch,
                CommandArgs = cmd
            };
        }
    }
}
