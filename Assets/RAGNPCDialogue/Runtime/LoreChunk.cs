using System;

namespace RAGNPCDialogue
{
    [Serializable]
    public class LoreChunk
    {
        public string sourceFile;
        public int chunkIndex;
        public string text;
        public int score;
    }
}
