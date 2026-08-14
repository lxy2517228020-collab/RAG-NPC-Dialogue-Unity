using UnityEngine;

namespace RAGNPCDialogue
{
    public class NPCDialogueSource : MonoBehaviour
    {
        [SerializeField] private DialogueSet dialogueSet;
        [SerializeField] private string npcDisplayName;

        public DialogueSet DialogueSet => dialogueSet;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(npcDisplayName))
                {
                    return npcDisplayName;
                }

                if (dialogueSet != null && !string.IsNullOrWhiteSpace(dialogueSet.npcName))
                {
                    return dialogueSet.npcName;
                }

                return gameObject.name;
            }
        }
    }
}
