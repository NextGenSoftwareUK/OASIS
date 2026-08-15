using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web6.Core.Helpers
{
    /// <summary>
    /// Splits a document into overlapping text chunks suitable for holonic storage and semantic search.
    /// Uses a sliding-window strategy with configurable chunk size and overlap so that no context is
    /// lost at chunk boundaries.
    /// </summary>
    public static class DocumentChunker
    {
        /// <summary>
        /// Splits <paramref name="text"/> into overlapping word-boundary chunks.
        /// </summary>
        /// <param name="text">The raw document text.</param>
        /// <param name="chunkTokens">Approximate tokens per chunk (4 chars ≈ 1 token). Default 400 tokens ≈ 1600 chars.</param>
        /// <param name="overlapTokens">Overlap in tokens between consecutive chunks. Default 50 tokens ≈ 200 chars.</param>
        public static List<string> Chunk(string text, int chunkTokens = 400, int overlapTokens = 50)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // Convert token targets to character targets (≈4 chars per token)
            int chunkChars   = chunkTokens   * 4;
            int overlapChars = overlapTokens * 4;

            var chunks = new List<string>();
            int start  = 0;
            int len    = text.Length;

            while (start < len)
            {
                int end = Math.Min(start + chunkChars, len);

                // Extend to the next word boundary so we don't cut mid-word
                if (end < len)
                {
                    int ws = text.IndexOfAny(new[] { ' ', '\n', '\r', '\t' }, end);
                    if (ws > 0 && ws - end < 200) // don't search too far ahead
                        end = ws;
                }

                string chunk = text.Substring(start, end - start).Trim();
                if (chunk.Length > 0)
                    chunks.Add(chunk);

                // Next window starts (chunkChars - overlapChars) forward from here
                int advance = chunkChars - overlapChars;
                if (advance <= 0) advance = chunkChars; // safety: never go backward
                start += advance;
            }

            return chunks;
        }
    }
}
