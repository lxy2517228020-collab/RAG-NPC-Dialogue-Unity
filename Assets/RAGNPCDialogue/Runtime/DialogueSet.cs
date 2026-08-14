using System.Collections.Generic;
using UnityEngine;

namespace RAGNPCDialogue
{
    [CreateAssetMenu(
        fileName = "NewDialogueSet",
        menuName = "RAG NPC Dialogue/Dialogue Set")]
    public class DialogueSet : ScriptableObject
    {
        public string npcName;
        public string scenario;
        public DialogueCategory dialogueCategory;
        public List<DialogueLine> dialogueLines = new List<DialogueLine>();
        public string generationSource = "MockLLM";
    }
}
