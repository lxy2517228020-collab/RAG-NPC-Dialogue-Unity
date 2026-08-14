using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RAGNPCDialogue.Editor
{
    public class TextChunker
    {
        private const int DefaultTargetChunkSize = 450;
        private const int DefaultMaximumChunkSize = 650;

        public List<LoreChunk> ChunkDocuments(
            IReadOnlyList<LoreDocument> documents,
            int targetChunkSize = DefaultTargetChunkSize,
            int maximumChunkSize = DefaultMaximumChunkSize)
        {
            List<LoreChunk> chunks = new List<LoreChunk>();

            foreach (LoreDocument document in documents)
            {
                int chunkIndex = 0;
                foreach (string chunkText in ChunkDocumentText(document.content, targetChunkSize, maximumChunkSize))
                {
                    if (string.IsNullOrWhiteSpace(chunkText))
                    {
                        continue;
                    }

                    chunks.Add(new LoreChunk
                    {
                        sourceFile = document.sourceFile,
                        chunkIndex = chunkIndex,
                        text = chunkText.Trim(),
                        score = 0
                    });
                    chunkIndex++;
                }
            }

            return chunks;
        }

        private static IEnumerable<string> ChunkDocumentText(
            string text,
            int targetChunkSize,
            int maximumChunkSize)
        {
            // Phase 4A keeps chunking beginner-friendly: split on blank-line paragraphs,
            // then group nearby paragraphs until the chunk is large enough for retrieval.
            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] paragraphs = Regex.Split(normalized, @"\n\s*\n+");
            StringBuilder currentChunk = new StringBuilder();

            foreach (string paragraph in paragraphs)
            {
                string trimmedParagraph = paragraph.Trim();
                if (string.IsNullOrWhiteSpace(trimmedParagraph))
                {
                    continue;
                }

                if (trimmedParagraph.Length > maximumChunkSize)
                {
                    if (currentChunk.Length > 0)
                    {
                        yield return currentChunk.ToString();
                        currentChunk.Clear();
                    }

                    foreach (string splitChunk in SplitLargeParagraph(trimmedParagraph, maximumChunkSize))
                    {
                        yield return splitChunk;
                    }

                    continue;
                }

                int projectedLength = currentChunk.Length + trimmedParagraph.Length + 2;
                if (currentChunk.Length > 0 && projectedLength > targetChunkSize)
                {
                    yield return currentChunk.ToString();
                    currentChunk.Clear();
                }

                if (currentChunk.Length > 0)
                {
                    currentChunk.AppendLine();
                    currentChunk.AppendLine();
                }

                currentChunk.Append(trimmedParagraph);
            }

            if (currentChunk.Length > 0)
            {
                yield return currentChunk.ToString();
            }
        }

        private static IEnumerable<string> SplitLargeParagraph(string paragraph, int maximumChunkSize)
        {
            int start = 0;
            while (start < paragraph.Length)
            {
                int length = System.Math.Min(maximumChunkSize, paragraph.Length - start);
                yield return paragraph.Substring(start, length).Trim();
                start += length;
            }
        }
    }
}
