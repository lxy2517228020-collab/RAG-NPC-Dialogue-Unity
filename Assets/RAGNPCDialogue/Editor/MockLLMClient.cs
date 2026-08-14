using System.Collections.Generic;
using System.Threading.Tasks;

namespace RAGNPCDialogue.Editor
{
    public class MockLLMClient : ILLMClient
    {
        public Task<LLMGenerationResult> GenerateDialogueAsync(
            NPCProfile profile,
            DialogueCategory category,
            string scenario,
            string prompt)
        {
            string npcName = string.IsNullOrWhiteSpace(profile.npcName) ? "NPC" : profile.npcName.Trim();
            string role = string.IsNullOrWhiteSpace(profile.role) ? "local" : profile.role.Trim();
            string faction = string.IsNullOrWhiteSpace(profile.faction) ? "no faction" : profile.faction.Trim();
            string style = string.IsNullOrWhiteSpace(profile.speakingStyle) ? "plainly" : profile.speakingStyle.Trim();

            List<DialogueLine> lines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    speaker = npcName,
                    text = BuildOpeningLine(npcName, role, category, scenario),
                    emotionOrTag = GetTagForCategory(category)
                },
                new DialogueLine
                {
                    speaker = npcName,
                    text = $"I speak {style}, and I stand with {faction}. Keep that in mind.",
                    emotionOrTag = "Character"
                },
                new DialogueLine
                {
                    speaker = npcName,
                    text = BuildClosingLine(category),
                    emotionOrTag = "Response"
                }
            };

            return Task.FromResult(LLMGenerationResult.Success(lines, "MockLLM"));
        }

        private static string BuildOpeningLine(
            string npcName,
            string role,
            DialogueCategory category,
            string scenario)
        {
            switch (category)
            {
                case DialogueCategory.Greeting:
                    return $"Name's {npcName}. If you need a {role}, you've found one.";
                case DialogueCategory.QuestIntroduction:
                    return $"I've got work that needs doing. Listen close: {scenario}.";
                case DialogueCategory.QuestCompletion:
                    return $"So, you handled it. Not bad for someone who walked in asking about {scenario}.";
                case DialogueCategory.Battle:
                    return $"No more talk. If {scenario} brought you here, then draw steel.";
                case DialogueCategory.Farewell:
                    return $"We're done for now. Remember what I said about {scenario}.";
                default:
                    return $"If you're here about {scenario}, you've come to the right {role}.";
            }
        }

        private static string BuildClosingLine(DialogueCategory category)
        {
            switch (category)
            {
                case DialogueCategory.Greeting:
                    return "Say what you came to say.";
                case DialogueCategory.QuestIntroduction:
                    return "Come back when the job is finished.";
                case DialogueCategory.QuestCompletion:
                    return "You've earned my respect, and maybe a little trust.";
                case DialogueCategory.Battle:
                    return "Let's see what you're made of.";
                case DialogueCategory.Farewell:
                    return "Safe roads, if such a thing still exists.";
                default:
                    return "That's the plain truth of it.";
            }
        }

        private static string GetTagForCategory(DialogueCategory category)
        {
            switch (category)
            {
                case DialogueCategory.Battle:
                    return "Aggressive";
                case DialogueCategory.Farewell:
                    return "Closing";
                case DialogueCategory.Greeting:
                    return "Greeting";
                case DialogueCategory.QuestIntroduction:
                    return "Quest";
                case DialogueCategory.QuestCompletion:
                    return "Reward";
                default:
                    return "Neutral";
            }
        }
    }
}
