# Interview Architecture

## 30-second explanation

This project adds a RAG-powered NPC dialogue generator to a Unity 2D platformer. A designer can define an NPC profile, retrieve relevant local lore, generate grounded dialogue with Gemini, save it as a ScriptableObject, and display it in the game.

## 2-minute explanation

The problem is that generic LLM dialogue may sound good but contradict game lore. The architecture solves this by loading local lore documents, splitting them into chunks, retrieving relevant chunks with keyword or semantic retrieval, and inserting those chunks into the prompt before generation.

The implementation is intentionally simple: lore is stored as `.txt`, chunked in C#, embedded with Gemini when semantic mode is selected, compared with cosine similarity, and passed into a PromptBuilder. The result is saved as a DialogueSet ScriptableObject that the runtime NPC dialogue presenter can display.

The result is a portfolio-ready Unity Editor tool that demonstrates No-RAG, Keyword-RAG, and Semantic-RAG in one workflow.

## Core execution flow

After `Generate Real API Dialogue`:

1. The Editor reads the current NPC profile and scenario.
2. If retrieval mode is `None`, no lore is retrieved.
3. If retrieval mode is `Keyword`, the keyword retriever scores lore chunks by word overlap.
4. If retrieval mode is `Semantic`, the query is embedded and compared to lore chunk embeddings.
5. Top-K lore chunks are passed into PromptBuilder.
6. PromptBuilder creates a grounded prompt.
7. The OpenAI-compatible client sends the prompt to Gemini.
8. The JSON response is parsed into DialogueLine objects.
9. The user saves the result as a DialogueSet.

## Five most important files

- `LoreDocumentLoader.cs`: Finds `.txt` lore files and loads their content.
- `TextChunker.cs`: Splits lore into small chunks suitable for retrieval.
- `GeminiEmbeddingService.cs`: Converts text into Gemini embedding vectors.
- `SemanticLoreRetriever.cs`: Calculates cosine similarity and returns Top-K lore chunks.
- `PromptBuilder.cs`: Combines NPC profile, retrieved lore, scenario, and generation rules.

## Important terms

- **RAG**: Retrieval-Augmented Generation. Retrieve relevant facts before asking the LLM to generate.
- **Embedding**: A numeric vector representing the meaning of text.
- **Chunk**: A small piece of a larger lore document.
- **Cosine similarity**: A score for how similar two vectors are in direction.
- **Top-K**: The K highest-scoring retrieved chunks.
- **Hallucination**: When an LLM invents unsupported information.
- **Prompt engineering**: Structuring input so the LLM follows rules and uses context.

## Likely interview questions

1. **Why use RAG?**  
   To ground generated dialogue in local game lore and reduce unsupported world facts.

2. **Why not fine-tune the model?**  
   Fine-tuning is heavier and less flexible. RAG lets designers update lore files without retraining.

3. **Why use embeddings?**  
   Embeddings let the system compare meaning, not just exact matching words.

4. **What is cosine similarity?**  
   It compares the direction of two vectors. Higher similarity means more semantic relevance.

5. **Why use Top-K?**  
   The prompt should include only the most relevant chunks so it stays focused.

6. **Why chunk documents?**  
   Smaller chunks are easier to score and avoid stuffing unrelated lore into the prompt.

7. **Why is Semantic better than Keyword?**  
   Semantic retrieval can connect related meanings like rebels and resistance members.

8. **Why keep Keyword mode?**  
   It is simple, fast, explainable, and useful as a baseline comparison.

9. **Why use an in-memory index?**  
   The demo has a tiny lore set, so a simple in-memory list is enough.

10. **Why not use FAISS or Chroma?**  
   That would add complexity beyond this demo's learning goal.

11. **How is API key security handled?**  
   Keys are stored in Unity EditorPrefs or environment variables, not committed assets.

12. **What happens if retrieval finds irrelevant lore?**  
   The preview exposes retrieved chunks so the designer can inspect and adjust lore or scenario.

13. **How do you reduce hallucination?**  
   Retrieved lore is inserted as canonical context and the prompt tells the model not to contradict it.

14. **What are current limitations?**  
   Small lore set, no persistent vector DB, no dialogue memory, no branching dialogue.

15. **How would you scale the project?**  
   Add persistent indexing, larger lore management, caching, richer authoring UI, and conversation memory.
