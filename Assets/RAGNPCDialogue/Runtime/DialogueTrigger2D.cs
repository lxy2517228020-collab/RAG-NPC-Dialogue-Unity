using UnityEngine;

namespace RAGNPCDialogue
{
    [RequireComponent(typeof(Collider2D))]
    public class DialogueTrigger2D : MonoBehaviour
    {
        [SerializeField] private NPCDialogueSource dialogueSource;
        [SerializeField] private DialoguePresenter dialoguePresenter;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;

        private bool playerInRange;

        private void Reset()
        {
            dialogueSource = GetComponent<NPCDialogueSource>();
            Collider2D triggerCollider = GetComponent<Collider2D>();
            triggerCollider.isTrigger = true;
        }

        private void Awake()
        {
            if (dialogueSource == null)
            {
                dialogueSource = GetComponent<NPCDialogueSource>();
            }

            if (dialoguePresenter == null)
            {
                dialoguePresenter = DialoguePresenter.Instance;
            }

            Collider2D triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null && !triggerCollider.isTrigger)
            {
                Debug.LogWarning($"{name} DialogueTrigger2D collider was not marked as trigger. Enabling trigger mode.");
                triggerCollider.isTrigger = true;
            }
        }

        private void Update()
        {
            if (!playerInRange || !Input.GetKeyDown(interactionKey))
            {
                return;
            }

            if (dialoguePresenter == null)
            {
                dialoguePresenter = DialoguePresenter.Instance;
            }

            if (dialoguePresenter == null)
            {
                Debug.LogWarning("Cannot interact because DialoguePresenter reference is missing.");
                return;
            }

            if (dialoguePresenter.IsDialogueActive)
            {
                dialoguePresenter.AdvanceDialogue();
                return;
            }

            if (dialogueSource == null)
            {
                Debug.LogWarning($"{name} cannot start dialogue because NPCDialogueSource is missing.");
                return;
            }

            if (dialogueSource.DialogueSet == null)
            {
                Debug.LogWarning($"{dialogueSource.DisplayName} has no DialogueSet assigned.");
                return;
            }

            dialoguePresenter.StartDialogue(dialogueSource.DialogueSet);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player"))
            {
                return;
            }

            playerInRange = true;
            Debug.Log($"Player entered NPC range: {(dialogueSource != null ? dialogueSource.DisplayName : name)}");
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player"))
            {
                return;
            }

            playerInRange = false;
            Debug.Log($"Player left NPC range: {(dialogueSource != null ? dialogueSource.DisplayName : name)}");
        }
    }
}
