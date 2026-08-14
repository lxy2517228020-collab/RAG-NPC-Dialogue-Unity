using System.Collections.Generic;
using System.Linq;

namespace RAGNPCDialogue.Editor
{
    public class SemanticLoreRetriever
    {
        public List<LoreChunk> Retrieve(
            IReadOnlyList<LoreVectorEntry> vectorEntries,
            float[] queryEmbedding,
            int topK)
        {
            if (vectorEntries == null || vectorEntries.Count == 0 || queryEmbedding == null || queryEmbedding.Length == 0 || topK <= 0)
            {
                return new List<LoreChunk>();
            }

            return vectorEntries
                .Select(entry => ScoreEntry(entry, queryEmbedding))
                .Where(chunk => chunk != null)
                .OrderByDescending(chunk => chunk.score)
                .ThenBy(chunk => chunk.sourceFile)
                .ThenBy(chunk => chunk.chunkIndex)
                .Take(topK)
                .ToList();
        }

        private static LoreChunk ScoreEntry(LoreVectorEntry entry, float[] queryEmbedding)
        {
            if (entry == null || entry.chunk == null || entry.embedding == null || entry.embedding.Length != queryEmbedding.Length)
            {
                return null;
            }

            float similarity = CosineSimilarity(queryEmbedding, entry.embedding);
            return new LoreChunk
            {
                sourceFile = entry.chunk.sourceFile,
                chunkIndex = entry.chunk.chunkIndex,
                text = entry.chunk.text,
                score = UnityEngine.Mathf.RoundToInt(similarity * 1000f)
            };
        }

        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            {
                return 0f;
            }

            float dotProduct = 0f;
            float magnitudeA = 0f;
            float magnitudeB = 0f;

            for (int i = 0; i < a.Length; i++)
            {
                // Dot product grows when both vectors point in similar directions.
                dotProduct += a[i] * b[i];

                // Vector magnitude is the length of each vector in embedding space.
                magnitudeA += a[i] * a[i];
                magnitudeB += b[i] * b[i];
            }

            if (magnitudeA <= 0f || magnitudeB <= 0f)
            {
                return 0f;
            }

            // Higher cosine similarity means the query and lore chunk point in a more similar semantic direction.
            return dotProduct / (UnityEngine.Mathf.Sqrt(magnitudeA) * UnityEngine.Mathf.Sqrt(magnitudeB));
        }
    }
}
