using TMPro;
using UnityEngine;

namespace RAGNPCDialogue
{
    public class DialoguePresenter : MonoBehaviour
    {
        public static DialoguePresenter Instance { get; private set; }

        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private TMP_Text continueText;

        private DialogueSet activeDialogueSet;
        private int currentLineIndex = -1;

        public bool IsDialogueActive => activeDialogueSet != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple DialoguePresenter instances found. The newest one will be used.");
            }

            Instance = this;
            CloseDialogue();
        }

        public bool StartDialogue(DialogueSet dialogueSet)
        {
            if (IsDialogueActive)
            {
                Debug.LogWarning("Dialogue start ignored because dialogue is already active.");
                return false;
            }

            if (dialogueSet == null)
            {
                Debug.LogWarning("Cannot start dialogue because DialogueSet is missing.");
                return false;
            }

            if (dialogueSet.dialogueLines == null || dialogueSet.dialogueLines.Count == 0)
            {
                Debug.LogWarning($"Cannot start dialogue for {dialogueSet.npcName}: DialogueSet has no lines.");
                return false;
            }

            if (dialoguePanel == null || speakerNameText == null || dialogueText == null)
            {
                Debug.LogWarning("Cannot start dialogue because DialoguePresenter UI references are missing.");
                return false;
            }

            activeDialogueSet = dialogueSet;
            currentLineIndex = 0;
            dialoguePanel.SetActive(true);

            if (continueText != null)
            {
                continueText.text = "Press E to continue";
            }

            Debug.Log($"Dialogue started: {dialogueSet.npcName}");
            ShowCurrentLine();
            return true;
        }

        public void AdvanceDialogue()
        {
            if (!IsDialogueActive)
            {
                return;
            }

            currentLineIndex++;

            if (currentLineIndex >= activeDialogueSet.dialogueLines.Count)
            {
                Debug.Log("Dialogue finished.");
                CloseDialogue();
                return;
            }

            Debug.Log($"Dialogue line index changed: {currentLineIndex}");
            ShowCurrentLine();
        }

        public void CloseDialogue()
        {
            activeDialogueSet = null;
            currentLineIndex = -1;

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (speakerNameText != null)
            {
                speakerNameText.text = string.Empty;
            }

            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
            }
        }

        private void ShowCurrentLine()
        {
            if (!IsDialogueActive || currentLineIndex < 0 || currentLineIndex >= activeDialogueSet.dialogueLines.Count)
            {
                Debug.LogWarning("Cannot show dialogue line because the current line index is invalid.");
                return;
            }

            DialogueLine line = activeDialogueSet.dialogueLines[currentLineIndex];
            speakerNameText.text = string.IsNullOrWhiteSpace(line.speaker) ? activeDialogueSet.npcName : line.speaker;
            dialogueText.text = line.text;
        }
    }
}
