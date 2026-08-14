using UnityEngine;

namespace RAGNPCDialogue
{
    [CreateAssetMenu(
        fileName = "NewNPCProfile",
        menuName = "RAG NPC Dialogue/NPC Profile")]
    public class NPCProfileAsset : ScriptableObject
    {
        public NPCProfile profile = new NPCProfile();
    }
}
