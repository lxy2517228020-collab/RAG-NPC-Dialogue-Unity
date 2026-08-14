using System;

namespace RAGNPCDialogue
{
    [Serializable]
    public class LoreVectorEntry
    {
        public LoreChunk chunk;
        public float[] embedding;
    }
}
