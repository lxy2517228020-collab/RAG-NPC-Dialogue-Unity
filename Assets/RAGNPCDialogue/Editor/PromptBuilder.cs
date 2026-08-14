using System.Text;
using System.Collections.Generic;

namespace RAGNPCDialogue.Editor
{
    public class PromptBuilder
    {
        public string BuildPrompt(
            NPCProfile profile,
            DialogueCategory category,
            string scenario,
            IReadOnlyList<LoreChunk> retrievedLoreChunks = null,
            bool includeRetrievedLoreSection = true)
        {
            StringBuilder prompt = new StringBuilder();

            prompt.AppendLine("NPC PROFILE");
            prompt.AppendLine($"Name: {profile.npcName}");
            prompt.AppendLine($"Role: {profile.role}");
            prompt.AppendLine($"Personality: {profile.personality}");
            prompt.AppendLine($"Faction: {profile.faction}");
            prompt.AppendLine($"Speaking Style: {profile.speakingStyle}");
            prompt.AppendLine($"Relationship To Player: {profile.relationshipToPlayer}");
            prompt.AppendLine($"Background: {profile.background}");
            prompt.AppendLine();

            if (includeRetrievedLoreSection)
            {
                prompt.AppendLine("RETRIEVED GAME LORE");
                if (retrievedLoreChunks == null || retrievedLoreChunks.Count == 0)
                {
                    prompt.AppendLine("No retrieved lore is available.");
                }
                else
                {
                    // Retrieved lore is placed before the scenario so the LLM treats it as canonical grounding context.
                    foreach (LoreChunk chunk in retrievedLoreChunks)
                    {
                        prompt.AppendLine($"[Source: {chunk.sourceFile}, Chunk: {chunk.chunkIndex}, Score: {chunk.score}]");
                        prompt.AppendLine(chunk.text);
                        prompt.AppendLine();
                    }
                }
                prompt.AppendLine();
            }

            prompt.AppendLine("SCENARIO");
            prompt.AppendLine(scenario);
            prompt.AppendLine();

            prompt.AppendLine("DIALOGUE CATEGORY");
            prompt.AppendLine(category.ToString());
            prompt.AppendLine();

            prompt.AppendLine("GENERATION INSTRUCTIONS");
            prompt.AppendLine("Use retrieved lore as canonical game information.");
            prompt.AppendLine("Do not contradict retrieved lore.");
            prompt.AppendLine("Do not invent major world facts when relevant lore is provided.");
            prompt.AppendLine("Stay in character.");
            prompt.AppendLine("Match the NPC speaking style.");
            prompt.AppendLine("Do not mention being an AI.");
            prompt.AppendLine("Do not mention retrieval, prompts, lore files, or being an AI.");
            prompt.AppendLine("Generate concise game dialogue.");
            prompt.AppendLine("Avoid unnecessary narration.");
            prompt.AppendLine("Output exactly 3 dialogue lines for this phase.");
            prompt.AppendLine("Return machine-readable structured output only.");
            prompt.AppendLine("Use this JSON shape:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"lines\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"speaker\": \"NPC name\",");
            prompt.AppendLine("      \"text\": \"Dialogue text\",");
            prompt.AppendLine("      \"emotion\": \"neutral\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");

            return prompt.ToString();
        }
    }
}
