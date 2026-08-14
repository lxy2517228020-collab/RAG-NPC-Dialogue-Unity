using System.Collections.Generic;
using System.Threading.Tasks;

namespace RAGNPCDialogue.Editor
{
    public interface ILLMClient
    {
        Task<LLMGenerationResult> GenerateDialogueAsync(
            NPCProfile profile,
            DialogueCategory category,
            string scenario,
            string prompt);
    }

    public class LLMGenerationResult
    {
        public bool success;
        public string errorMessage;
        public string rawResponse;
        public string generationSource;
        public List<DialogueLine> dialogueLines = new List<DialogueLine>();

        public static LLMGenerationResult Success(
            List<DialogueLine> dialogueLines,
            string generationSource,
            string rawResponse = "")
        {
            return new LLMGenerationResult
            {
                success = true,
                dialogueLines = dialogueLines,
                generationSource = generationSource,
                rawResponse = rawResponse
            };
        }

        public static LLMGenerationResult Failure(string errorMessage, string rawResponse = "")
        {
            return new LLMGenerationResult
            {
                success = false,
                errorMessage = errorMessage,
                rawResponse = rawResponse
            };
        }
    }
}
