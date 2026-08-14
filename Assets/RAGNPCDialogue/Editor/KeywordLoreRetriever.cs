using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RAGNPCDialogue.Editor
{
    public class KeywordLoreRetriever
    {
        private static readonly HashSet<string> StopWords = new HashSet<string>
        {
            "a", "an", "and", "are", "as", "at", "be", "but", "by", "for", "from",
            "he", "her", "his", "i", "in", "is", "it", "its", "of", "on", "or",
            "player", "she", "that", "the", "their", "them", "they", "this", "to",
            "what", "when", "where", "who", "with", "you", "your"
        };

        public List<LoreChunk> Retrieve(
            NPCProfile profile,
            string scenario,
            DialogueCategory category,
            IReadOnlyList<LoreChunk> chunks,
            int topK)
        {
            if (chunks == null || chunks.Count == 0 || topK <= 0)
            {
                return new List<LoreChunk>();
            }

            List<string> queryTerms = BuildQueryTerms(profile, scenario, category);
            string npcName = profile != null ? Normalize(profile.npcName) : string.Empty;

            return chunks
                // Top-K retrieval means score every chunk, sort best first, then keep only the most relevant K chunks.
                .Select(chunk => ScoreChunk(chunk, queryTerms, npcName))
                .Where(chunk => chunk.score > 0)
                .OrderByDescending(chunk => chunk.score)
                .ThenBy(chunk => chunk.sourceFile)
                .ThenBy(chunk => chunk.chunkIndex)
                .Take(topK)
                .ToList();
        }

        private static List<string> BuildQueryTerms(
            NPCProfile profile,
            string scenario,
            DialogueCategory category)
        {
            string query = string.Join(" ", new[]
            {
                profile != null ? profile.npcName : string.Empty,
                profile != null ? profile.role : string.Empty,
                profile != null ? profile.faction : string.Empty,
                profile != null ? profile.background : string.Empty,
                scenario,
                category.ToString()
            });

            return Tokenize(query)
                .Where(term => term.Length > 2 && !StopWords.Contains(term))
                .Distinct()
                .ToList();
        }

        private static LoreChunk ScoreChunk(
            LoreChunk chunk,
            IReadOnlyList<string> queryTerms,
            string npcName)
        {
            string normalizedChunk = Normalize(chunk.text);
            int score = 0;

            foreach (string term in queryTerms)
            {
                if (normalizedChunk.Contains(term))
                {
                    score++;
                }
            }

            if (!string.IsNullOrWhiteSpace(npcName) && normalizedChunk.Contains(npcName))
            {
                score += 3;
            }

            return new LoreChunk
            {
                sourceFile = chunk.sourceFile,
                chunkIndex = chunk.chunkIndex,
                text = chunk.text,
                score = score
            };
        }

        private static IEnumerable<string> Tokenize(string text)
        {
            return Regex.Split(Normalize(text), @"[^a-z0-9']+")
                .Where(token => !string.IsNullOrWhiteSpace(token));
        }

        private static string Normalize(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text.ToLowerInvariant();
        }
    }
}
