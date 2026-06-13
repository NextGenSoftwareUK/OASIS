using System;

namespace NextGenSoftware.OASIS.STAR.CLI
{
    /// <summary>
    /// Parses <c>search</c> argv: criteria may be multiple tokens; optional positive int <paramref name="maxResults"/>
    /// is the last token if it parses as an integer (per-command cap). Merged with <c>--search-limit</c> in the router.
    /// </summary>
    internal static class StarCliStarnetSearchArgv
    {
        public static bool TryParse(string[] inputArgs, out string criteria, out int maxResults, out string errorMessage)
        {
            criteria = null;
            maxResults = 0;
            errorMessage = null;

            if (inputArgs == null || inputArgs.Length == 0)
            {
                errorMessage = "Missing command arguments.";
                return false;
            }

            int idx = -1;
            for (int i = 0; i < inputArgs.Length; i++)
            {
                if (string.Equals(inputArgs[i], "search", StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0 || idx + 1 >= inputArgs.Length)
            {
                errorMessage = "search requires a criteria token.";
                return false;
            }

            var tail = new string[inputArgs.Length - (idx + 1)];
            Array.Copy(inputArgs, idx + 1, tail, 0, tail.Length);
            if (tail.Length == 0)
            {
                errorMessage = "search requires a criteria token.";
                return false;
            }

            if (tail.Length >= 2
                && int.TryParse(tail[tail.Length - 1], out int lim)
                && lim > 0)
            {
                maxResults = lim;
                criteria = string.Join(" ", tail, 0, tail.Length - 1).Trim();
            }
            else
                criteria = string.Join(" ", tail).Trim();

            if (string.IsNullOrWhiteSpace(criteria))
            {
                errorMessage = "search criteria must be non-empty.";
                return false;
            }

            return true;
        }
    }
}
