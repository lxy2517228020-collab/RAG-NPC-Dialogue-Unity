using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace RAGNPCDialogue.Editor
{
    public class NPCDialogueGeneratorWindow : EditorWindow
    {
        private const string DialogueSetFolder = "Assets/RAGNPCDialogue/Generated/DialogueSets";
        private const string NPCProfileFolder = "Assets/RAGNPCDialogue/Generated/NPCProfiles";

        private readonly PromptBuilder promptBuilder = new PromptBuilder();
        private readonly LoreDocumentLoader loreDocumentLoader = new LoreDocumentLoader();
        private readonly TextChunker textChunker = new TextChunker();
        private readonly KeywordLoreRetriever keywordLoreRetriever = new KeywordLoreRetriever();
        private readonly SemanticLoreRetriever semanticLoreRetriever = new SemanticLoreRetriever();
        private const string EmbeddingModelEditorPrefsKey = "RAGNPCDialogue.GeminiEmbedding.Model";

        private NPCProfile profile = new NPCProfile();
        private NPCProfileAsset selectedProfileAsset;
        private DialogueCategory dialogueCategory = DialogueCategory.NormalConversation;
        private GenerationMode generationMode = GenerationMode.Mock;
        private string scenario = string.Empty;
        private string baseUrl = string.Empty;
        private string modelName = string.Empty;
        private string apiKey = string.Empty;
        private string embeddingModelName = "gemini-embedding-001";
        private string generatedPrompt = string.Empty;
        private string rawApiResponse = string.Empty;
        private string generationSource = "MockLLM";
        private List<DialogueLine> generatedDialogue = new List<DialogueLine>();
        private List<LoreDocument> loreDocuments = new List<LoreDocument>();
        private List<LoreChunk> loreChunks = new List<LoreChunk>();
        private List<LoreChunk> retrievedLoreChunks = new List<LoreChunk>();
        private List<ComparisonResult> comparisonResults = new List<ComparisonResult>();
        private List<LoreVectorEntry> semanticIndex = new List<LoreVectorEntry>();
        private RetrievalMode retrievalMode = RetrievalMode.None;
        private int topK = 3;
        private bool loreIndexBuilt;
        private bool semanticIndexBuilt;
        private int embeddingDimension;
        private Vector2 scrollPosition;
        private Vector2 rawApiResponseScrollPosition;
        private string statusMessage = string.Empty;
        private MessageType statusMessageType = MessageType.Info;
        private bool isGenerating;
        private GUIStyle wrappedTextAreaStyle;
        private GUIStyle wrappedBoxStyle;
        private GUIStyle wrappedMiniLabelStyle;

        private enum GenerationMode
        {
            Mock,
            RealAPI
        }

        private enum RetrievalMode
        {
            None,
            Keyword,
            Semantic
        }

        [MenuItem("Tools/AI Tools/NPC Dialogue Generator")]
        public static void OpenWindow()
        {
            NPCDialogueGeneratorWindow window = GetWindow<NPCDialogueGeneratorWindow>();
            window.titleContent = new GUIContent("NPC Dialogue Generator");
            window.minSize = new Vector2(420f, 600f);
            window.Show();
        }

        private void OnEnable()
        {
            baseUrl = ApiKeyProvider.GetBaseUrl();
            modelName = ApiKeyProvider.GetModel();
            apiKey = ApiKeyProvider.GetApiKey();
            embeddingModelName = EditorPrefs.GetString(EmbeddingModelEditorPrefsKey, embeddingModelName);
        }

        private void OnGUI()
        {
            EnsureStyles();

            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition,
                false,
                true,
                GUILayout.ExpandWidth(true));

            DrawProfileAssetControls();
            DrawProfileFields();
            DrawLoreRetrievalSection();
            DrawRagComparisonDemoSection();
            DrawApiConfiguration();
            DrawGenerationControls();
            DrawGeneratedOutput();
            DrawSaveControls();

            EditorGUILayout.EndScrollView();
        }

        private void DrawProfileAssetControls()
        {
            EditorGUILayout.LabelField("NPC Profile Asset", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            NPCProfileAsset newProfileAsset = (NPCProfileAsset)EditorGUILayout.ObjectField(
                "NPC Profile Asset",
                selectedProfileAsset,
                typeof(NPCProfileAsset),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                selectedProfileAsset = newProfileAsset;
                LoadSelectedProfileAsset();
            }

            if (GUILayout.Button("New Profile"))
            {
                CreateNewProfileAsset();
            }

            if (GUILayout.Button("Save Profile"))
            {
                SaveProfileAsset();
            }

            EditorGUILayout.Space(8f);
        }

        private void DrawProfileFields()
        {
            EditorGUILayout.LabelField("NPC Profile", EditorStyles.boldLabel);
            profile.npcName = EditorGUILayout.TextField("NPC Name", profile.npcName);
            profile.role = EditorGUILayout.TextField("Role", profile.role);
            profile.personality = EditorGUILayout.TextField("Personality", profile.personality);
            profile.faction = EditorGUILayout.TextField("Faction", profile.faction);
            profile.speakingStyle = EditorGUILayout.TextField("Speaking Style", profile.speakingStyle);
            profile.relationshipToPlayer = EditorGUILayout.TextField("Relationship To Player", profile.relationshipToPlayer);

            EditorGUILayout.LabelField("Background");
            profile.background = DrawWrappedTextArea(
                profile.background,
                60f);

            EditorGUILayout.Space(8f);
            dialogueCategory = (DialogueCategory)EditorGUILayout.EnumPopup("Dialogue Category", dialogueCategory);

            EditorGUILayout.LabelField("Scenario");
            scenario = DrawWrappedTextArea(
                scenario,
                60f);
        }

        private void DrawApiConfiguration()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Generation Mode", EditorStyles.boldLabel);
            int selectedMode = GUILayout.Toolbar(
                generationMode == GenerationMode.Mock ? 0 : 1,
                new[] { "Mock", "Real API" });
            generationMode = selectedMode == 0 ? GenerationMode.Mock : GenerationMode.RealAPI;

            if (generationMode != GenerationMode.RealAPI)
            {
                EditorGUILayout.HelpBox(
                    "Mock mode runs fully offline and does not use network requests.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("OpenAI-Compatible API", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Enter a provider Base URL that supports chat completions. Example shape: https://provider.example/v1",
                MessageType.None);

            baseUrl = EditorGUILayout.TextField("Base URL", baseUrl);
            modelName = EditorGUILayout.TextField("Model Name", modelName);
            apiKey = EditorGUILayout.PasswordField("API Key", apiKey);

            if (GUILayout.Button("Save API Configuration"))
            {
                SaveApiConfiguration();
            }

            using (new EditorGUI.DisabledScope(isGenerating))
            {
                if (GUILayout.Button("Test Connection"))
                {
                    _ = TestConnectionAsync();
                }
            }
        }

        private void DrawLoreRetrievalSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Lore / Retrieval", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Lore Folder: {loreDocumentLoader.GetLoreFolder()}", wrappedMiniLabelStyle);
            EditorGUILayout.LabelField($"Documents Found: {loreDocuments.Count}");
            EditorGUILayout.LabelField($"Chunks Created: {loreChunks.Count}");
            retrievalMode = (RetrievalMode)EditorGUILayout.EnumPopup("Retrieval Mode", retrievalMode);
            topK = EditorGUILayout.IntSlider("Top-K", topK, 1, 8);

            if (retrievalMode == RetrievalMode.None)
            {
                EditorGUILayout.HelpBox(
                    "No RAG mode sends only the NPC profile and scenario to the generator.",
                    MessageType.Info);
            }
            else
            {
                if (GUILayout.Button("Reload Lore / Build Lore Index"))
                {
                    BuildLoreIndex();
                }

                if (retrievalMode == RetrievalMode.Semantic)
                {
                    DrawSemanticIndexControls();
                }

                using (new EditorGUI.DisabledScope(isGenerating))
                {
                    if (GUILayout.Button("Preview Retrieved Lore"))
                    {
                        _ = PreviewRetrievedLoreAsync();
                    }
                }
            }

            if (loreIndexBuilt && loreDocuments.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No .txt lore documents were found. Dialogue generation will continue without retrieved lore.",
                    MessageType.Warning);
            }

            if (loreIndexBuilt && loreDocuments.Count > 0 && retrievedLoreChunks.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No relevant lore chunks matched the current NPC profile and scenario yet.",
                    MessageType.Warning);
            }

            DrawRetrievedLoreChunks();
        }

        private void DrawSemanticIndexControls()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Semantic Embeddings", EditorStyles.boldLabel);
            embeddingModelName = EditorGUILayout.TextField("Embedding Model", embeddingModelName);
            EditorGUILayout.LabelField($"Indexed Chunks: {semanticIndex.Count}");
            EditorGUILayout.LabelField($"Embedding Dimension: {embeddingDimension}");

            using (new EditorGUI.DisabledScope(isGenerating))
            {
                if (GUILayout.Button("Build Semantic Index"))
                {
                    _ = BuildSemanticIndexAsync(true);
                }
            }

            EditorGUILayout.HelpBox(
                "Semantic mode uses the saved Gemini API key from API Configuration or environment variables.",
                MessageType.None);
        }

        private void DrawRagComparisonDemoSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("RAG Comparison Demo", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Primary demo scenario: Does Roland help the rebels at night?",
                wrappedMiniLabelStyle);

            using (new EditorGUI.DisabledScope(isGenerating))
            {
                if (GUILayout.Button("Run Comparison"))
                {
                    _ = RunComparisonAsync();
                }
            }

            DrawComparisonResults();
        }

        private void DrawGenerationControls()
        {
            EditorGUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(isGenerating))
            {
                string buttonLabel = generationMode == GenerationMode.Mock
                    ? "Generate Mock Dialogue"
                    : "Generate Real API Dialogue";

                if (GUILayout.Button(buttonLabel, GUILayout.Height(32f)))
                {
                    _ = GenerateDialogueAsync();
                }
            }

            if (isGenerating)
            {
                EditorGUILayout.HelpBox("Generating...", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusMessageType);
            }
        }

        private void DrawGeneratedOutput()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Generated Dialogue", EditorStyles.boldLabel);

            if (generatedDialogue.Count == 0)
            {
                EditorGUILayout.HelpBox("No dialogue generated yet.", MessageType.None);
            }
            else
            {
                DrawGeneratedDialogueLines();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Generated Prompt Debug", EditorStyles.boldLabel);
            generatedPrompt = DrawWrappedTextArea(
                generatedPrompt,
                180f);

            if (!string.IsNullOrWhiteSpace(rawApiResponse))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Raw API Response Debug", EditorStyles.boldLabel);
                rawApiResponseScrollPosition = EditorGUILayout.BeginScrollView(
                    rawApiResponseScrollPosition,
                    false,
                    true,
                    GUILayout.Height(190f),
                    GUILayout.ExpandWidth(true));
                rawApiResponse = DrawWrappedTextArea(rawApiResponse, 190f);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSaveControls()
        {
            EditorGUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(isGenerating))
            {
                if (GUILayout.Button("Save Dialogue", GUILayout.Height(32f)))
                {
                    SaveDialogueSet();
                }
            }
        }

        private async Task GenerateDialogueAsync()
        {
            if (!ValidateInputs())
            {
                return;
            }

            isGenerating = true;
            statusMessage = "Generating...";
            statusMessageType = MessageType.Info;
            rawApiResponse = string.Empty;
            Repaint();

            RetrievalResult retrievalResult = await RetrieveLoreForCurrentInputsAsync();
            if (!retrievalResult.success)
            {
                generatedDialogue.Clear();
                generatedPrompt = promptBuilder.BuildPrompt(
                    profile,
                    dialogueCategory,
                    scenario,
                    retrievedLoreChunks,
                    retrievalMode != RetrievalMode.None);
                SetError(retrievalResult.errorMessage);
                isGenerating = false;
                Repaint();
                return;
            }

            retrievedLoreChunks = retrievalResult.chunks;
            generatedPrompt = promptBuilder.BuildPrompt(
                profile,
                dialogueCategory,
                scenario,
                retrievedLoreChunks,
                retrievalMode != RetrievalMode.None);

            ILLMClient llmClient = CreateClientForCurrentMode();
            LLMGenerationResult result;
            try
            {
                result = await llmClient.GenerateDialogueAsync(
                    profile,
                    dialogueCategory,
                    scenario,
                    generatedPrompt);
            }
            catch (System.Exception exception)
            {
                result = LLMGenerationResult.Failure($"Generation failed: {exception.Message}");
            }

            rawApiResponse = result.rawResponse;

            if (!result.success)
            {
                generatedDialogue.Clear();
                generationSource = generationMode == GenerationMode.Mock ? "MockLLM" : $"OpenAICompatible:{modelName}";
                SetError(result.errorMessage);
                isGenerating = false;
                Repaint();
                return;
            }

            generatedDialogue = result.dialogueLines;
            generationSource = result.generationSource;

            statusMessage = generationMode == GenerationMode.Mock
                ? "Mock dialogue generated locally."
                : "Real API dialogue generated.";
            statusMessageType = MessageType.Info;
            isGenerating = false;
            Repaint();
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(profile.npcName))
            {
                SetError("NPC Name is required before generating dialogue.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(scenario))
            {
                SetError("Scenario is required before generating dialogue.");
                return false;
            }

            return true;
        }

        private void SaveDialogueSet()
        {
            if (generatedDialogue.Count == 0)
            {
                SetError("Generate dialogue before saving.");
                EditorUtility.DisplayDialog("Nothing To Save", "Generate dialogue before saving.", "OK");
                return;
            }

            EnsureDialogueSetFolderExists();

            DialogueSet dialogueSet = CreateInstance<DialogueSet>();
            dialogueSet.npcName = profile.npcName.Trim();
            dialogueSet.scenario = scenario.Trim();
            dialogueSet.dialogueCategory = dialogueCategory;
            dialogueSet.generationSource = generationSource;
            dialogueSet.dialogueLines = new List<DialogueLine>(generatedDialogue);

            string fileName = $"{SanitizeFileName(profile.npcName)}_{dialogueCategory}.asset";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{DialogueSetFolder}/{fileName}");

            AssetDatabase.CreateAsset(dialogueSet, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(dialogueSet);

            statusMessage = $"Saved DialogueSet asset: {path}";
            statusMessageType = MessageType.Info;
        }

        private static void EnsureDialogueSetFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/RAGNPCDialogue"))
            {
                AssetDatabase.CreateFolder("Assets", "RAGNPCDialogue");
            }

            if (!AssetDatabase.IsValidFolder("Assets/RAGNPCDialogue/Generated"))
            {
                AssetDatabase.CreateFolder("Assets/RAGNPCDialogue", "Generated");
            }

            if (!AssetDatabase.IsValidFolder(DialogueSetFolder))
            {
                AssetDatabase.CreateFolder("Assets/RAGNPCDialogue/Generated", "DialogueSets");
            }
        }

        private static void EnsureNPCProfileFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/RAGNPCDialogue"))
            {
                AssetDatabase.CreateFolder("Assets", "RAGNPCDialogue");
            }

            if (!AssetDatabase.IsValidFolder("Assets/RAGNPCDialogue/Generated"))
            {
                AssetDatabase.CreateFolder("Assets/RAGNPCDialogue", "Generated");
            }

            if (!AssetDatabase.IsValidFolder(NPCProfileFolder))
            {
                AssetDatabase.CreateFolder("Assets/RAGNPCDialogue/Generated", "NPCProfiles");
            }
        }

        private static string SanitizeFileName(string value)
        {
            string sanitized = value.Trim();

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(sanitized) ? "NPC" : sanitized.Replace(' ', '_');
        }

        private void LoadSelectedProfileAsset()
        {
            if (selectedProfileAsset == null)
            {
                return;
            }

            profile = CopyProfile(selectedProfileAsset.profile);
            statusMessage = $"Loaded NPC profile: {selectedProfileAsset.name}";
            statusMessageType = MessageType.Info;
        }

        private void CreateNewProfileAsset()
        {
            EnsureNPCProfileFolderExists();

            NPCProfileAsset asset = CreateInstance<NPCProfileAsset>();
            asset.profile = CopyProfile(profile);

            string displayName = string.IsNullOrWhiteSpace(profile.npcName) ? "NewNPC" : profile.npcName.Trim();
            string fileName = $"{SanitizeFileName(displayName)}_Profile.asset";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{NPCProfileFolder}/{fileName}");

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(asset);

            selectedProfileAsset = asset;
            statusMessage = $"Created NPC profile asset: {path}";
            statusMessageType = MessageType.Info;
        }

        private void SaveProfileAsset()
        {
            if (selectedProfileAsset == null)
            {
                CreateNewProfileAsset();
                return;
            }

            selectedProfileAsset.profile = CopyProfile(profile);
            EditorUtility.SetDirty(selectedProfileAsset);
            AssetDatabase.SaveAssets();

            statusMessage = $"Saved NPC profile: {selectedProfileAsset.name}";
            statusMessageType = MessageType.Info;
        }

        private static NPCProfile CopyProfile(NPCProfile source)
        {
            if (source == null)
            {
                return new NPCProfile();
            }

            return new NPCProfile
            {
                npcName = source.npcName,
                role = source.role,
                personality = source.personality,
                faction = source.faction,
                speakingStyle = source.speakingStyle,
                relationshipToPlayer = source.relationshipToPlayer,
                background = source.background
            };
        }

        private void DrawGeneratedDialogueLines()
        {
            foreach (DialogueLine line in generatedDialogue)
            {
                EditorGUILayout.BeginVertical(wrappedBoxStyle);
                EditorGUILayout.LabelField(
                    $"{line.speaker} [{line.emotionOrTag}]",
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField(line.text, wrappedMiniLabelStyle);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }
        }

        private void SetError(string message)
        {
            statusMessage = message;
            statusMessageType = MessageType.Error;
        }

        private ILLMClient CreateClientForCurrentMode()
        {
            if (generationMode == GenerationMode.Mock)
            {
                return new MockLLMClient();
            }

            return new OpenAICompatibleLLMClient(baseUrl, apiKey, modelName);
        }

        private void SaveApiConfiguration()
        {
            ApiKeyProvider.SaveBaseUrl(baseUrl);
            ApiKeyProvider.SaveModel(modelName);
            ApiKeyProvider.SaveApiKey(apiKey);

            statusMessage = "API configuration saved to EditorPrefs.";
            statusMessageType = MessageType.Info;
        }

        private async Task TestConnectionAsync()
        {
            if (generationMode != GenerationMode.RealAPI)
            {
                statusMessage = "Switch to Real API mode before testing the connection.";
                statusMessageType = MessageType.Warning;
                return;
            }

            NPCProfile testProfile = new NPCProfile
            {
                npcName = "Test NPC",
                role = "Guide",
                personality = "Helpful",
                faction = "Test",
                speakingStyle = "Brief",
                relationshipToPlayer = "Neutral",
                background = "Used only to test the configured API connection."
            };

            string testScenario = "The player asks whether the dialogue generator is connected.";
            string testPrompt = promptBuilder.BuildPrompt(testProfile, DialogueCategory.Greeting, testScenario);

            isGenerating = true;
            statusMessage = "Testing connection...";
            statusMessageType = MessageType.Info;
            rawApiResponse = string.Empty;
            Repaint();

            LLMGenerationResult result;
            try
            {
                result = await new OpenAICompatibleLLMClient(baseUrl, apiKey, modelName)
                    .GenerateDialogueAsync(testProfile, DialogueCategory.Greeting, testScenario, testPrompt);
            }
            catch (System.Exception exception)
            {
                result = LLMGenerationResult.Failure($"Connection test failed: {exception.Message}");
            }

            rawApiResponse = result.rawResponse;
            isGenerating = false;

            if (result.success)
            {
                statusMessage = "Connection test succeeded.";
                statusMessageType = MessageType.Info;
            }
            else
            {
                SetError(result.errorMessage);
            }

            Repaint();
        }

        private void EnsureStyles()
        {
            if (wrappedTextAreaStyle == null)
            {
                wrappedTextAreaStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    stretchWidth = true
                };
            }

            if (wrappedBoxStyle == null)
            {
                wrappedBoxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    wordWrap = true,
                    stretchWidth = true,
                    padding = new RectOffset(8, 8, 6, 6)
                };
            }

            if (wrappedMiniLabelStyle == null)
            {
                wrappedMiniLabelStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    wordWrap = true,
                    stretchWidth = true
                };
            }
        }

        private string DrawWrappedTextArea(string value, float minimumHeight)
        {
            float width = Mathf.Max(120f, position.width - 34f);
            float calculatedHeight = wrappedTextAreaStyle.CalcHeight(new GUIContent(value), width);
            float height = Mathf.Max(minimumHeight, calculatedHeight);
            Rect rect = EditorGUILayout.GetControlRect(
                false,
                height,
                GUILayout.ExpandWidth(true));

            rect.width = Mathf.Min(rect.width, width);
            return EditorGUI.TextArea(rect, value, wrappedTextAreaStyle);
        }

        private void BuildLoreIndex()
        {
            loreDocuments = loreDocumentLoader.LoadDocuments();
            loreChunks = textChunker.ChunkDocuments(loreDocuments);
            retrievedLoreChunks.Clear();
            semanticIndex.Clear();
            loreIndexBuilt = true;
            semanticIndexBuilt = false;
            embeddingDimension = 0;

            statusMessage = loreDocuments.Count == 0
                ? "No .txt lore documents found. Generation will continue without retrieved lore."
                : $"Lore index built: {loreDocuments.Count} documents, {loreChunks.Count} chunks.";
            statusMessageType = loreDocuments.Count == 0 ? MessageType.Warning : MessageType.Info;
        }

        private async Task PreviewRetrievedLoreAsync()
        {
            if (!ValidateInputs())
            {
                return;
            }

            isGenerating = true;
            statusMessage = "Retrieving lore...";
            statusMessageType = MessageType.Info;
            Repaint();

            RetrievalResult retrievalResult = await RetrieveLoreForCurrentInputsAsync();
            retrievedLoreChunks = retrievalResult.chunks;
            isGenerating = false;

            if (!retrievalResult.success)
            {
                SetError(retrievalResult.errorMessage);
                Repaint();
                return;
            }

            if (retrievedLoreChunks.Count == 0)
            {
                statusMessage = "No relevant lore chunks found for the current NPC profile and scenario.";
                statusMessageType = MessageType.Warning;
                return;
            }

            statusMessage = $"Retrieved {retrievedLoreChunks.Count} lore chunks.";
            statusMessageType = MessageType.Info;
            Repaint();
        }

        private async Task<RetrievalResult> RetrieveLoreForCurrentInputsAsync()
        {
            if (!loreIndexBuilt)
            {
                BuildLoreIndex();
            }

            if (retrievalMode == RetrievalMode.None)
            {
                return RetrievalResult.Success(new List<LoreChunk>());
            }

            if (loreChunks.Count == 0)
            {
                return RetrievalResult.Success(new List<LoreChunk>());
            }

            if (retrievalMode == RetrievalMode.Keyword)
            {
                return RetrievalResult.Success(keywordLoreRetriever.Retrieve(
                    profile,
                    scenario,
                    dialogueCategory,
                    loreChunks,
                    topK));
            }

            if (!semanticIndexBuilt || semanticIndex.Count == 0)
            {
                RetrievalResult buildResult = await BuildSemanticIndexAsync(false);
                if (!buildResult.success)
                {
                    return buildResult;
                }
            }

            string query = BuildRetrievalQuery();
            GeminiEmbeddingService.EmbeddingResult embeddingResult =
                await CreateEmbeddingService().EmbedTextAsync(query);

            if (!embeddingResult.success)
            {
                return RetrievalResult.Failure(embeddingResult.errorMessage);
            }

            if (embeddingResult.embedding == null || embeddingResult.embedding.Length == 0)
            {
                return RetrievalResult.Failure("Semantic query embedding was empty.");
            }

            List<LoreChunk> chunks = semanticLoreRetriever.Retrieve(
                semanticIndex,
                embeddingResult.embedding,
                topK);

            return RetrievalResult.Success(chunks);
        }

        private void DrawRetrievedLoreChunks()
        {
            if (retrievedLoreChunks.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Retrieved Lore Preview", EditorStyles.boldLabel);

            for (int i = 0; i < retrievedLoreChunks.Count; i++)
            {
                LoreChunk chunk = retrievedLoreChunks[i];
                EditorGUILayout.BeginVertical(wrappedBoxStyle);
                string scoreLabel = retrievalMode == RetrievalMode.Semantic
                    ? $"#{i + 1} Similarity: {(chunk.score / 1000f):0.000}"
                    : $"#{i + 1} Score: {chunk.score}";
                EditorGUILayout.LabelField(scoreLabel, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Source: {chunk.sourceFile}");
                EditorGUILayout.LabelField($"Chunk Index: {chunk.chunkIndex}");
                EditorGUILayout.LabelField(chunk.text, wrappedMiniLabelStyle);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }
        }

        private async Task<RetrievalResult> BuildSemanticIndexAsync(bool updateBusyState)
        {
            EditorPrefs.SetString(EmbeddingModelEditorPrefsKey, embeddingModelName);

            if (!loreIndexBuilt)
            {
                BuildLoreIndex();
            }

            if (loreChunks.Count == 0)
            {
                return RetrievalResult.Failure("No lore chunks are available to embed.");
            }

            semanticIndex.Clear();
            embeddingDimension = 0;
            semanticIndexBuilt = false;

            if (updateBusyState)
            {
                isGenerating = true;
            }
            statusMessage = "Building semantic index...";
            statusMessageType = MessageType.Info;
            Repaint();

            GeminiEmbeddingService embeddingService = CreateEmbeddingService();
            for (int i = 0; i < loreChunks.Count; i++)
            {
                LoreChunk chunk = loreChunks[i];
                statusMessage = $"Embedding lore chunk {i + 1} / {loreChunks.Count}...";
                Repaint();

                GeminiEmbeddingService.EmbeddingResult embeddingResult =
                    await embeddingService.EmbedTextAsync(chunk.text);

                if (!embeddingResult.success)
                {
                    semanticIndex.Clear();
                    embeddingDimension = 0;
                    if (updateBusyState)
                    {
                        isGenerating = false;
                    }
                    SetError(embeddingResult.errorMessage);
                    Repaint();
                    return RetrievalResult.Failure(embeddingResult.errorMessage);
                }

                if (embeddingResult.embedding == null || embeddingResult.embedding.Length == 0)
                {
                    semanticIndex.Clear();
                    embeddingDimension = 0;
                    if (updateBusyState)
                    {
                        isGenerating = false;
                    }
                    SetError("Embedding API returned an empty vector.");
                    Repaint();
                    return RetrievalResult.Failure("Embedding API returned an empty vector.");
                }

                if (embeddingDimension == 0)
                {
                    embeddingDimension = embeddingResult.embedding.Length;
                }
                else if (embeddingResult.embedding.Length != embeddingDimension)
                {
                    semanticIndex.Clear();
                    embeddingDimension = 0;
                    if (updateBusyState)
                    {
                        isGenerating = false;
                    }
                    SetError("Embedding vector size mismatch while building semantic index.");
                    Repaint();
                    return RetrievalResult.Failure("Embedding vector size mismatch while building semantic index.");
                }

                semanticIndex.Add(new LoreVectorEntry
                {
                    chunk = chunk,
                    embedding = embeddingResult.embedding
                });
            }

            semanticIndexBuilt = true;
            if (updateBusyState)
            {
                isGenerating = false;
            }
            statusMessage = $"Semantic index built: {semanticIndex.Count} chunks, {embeddingDimension} dimensions.";
            statusMessageType = MessageType.Info;
            Repaint();
            return RetrievalResult.Success(new List<LoreChunk>());
        }

        private GeminiEmbeddingService CreateEmbeddingService()
        {
            return new GeminiEmbeddingService(ApiKeyProvider.GetApiKey(), embeddingModelName);
        }

        private string BuildRetrievalQuery()
        {
            return string.Join(" ", new[]
            {
                profile.npcName,
                profile.role,
                profile.faction,
                scenario,
                dialogueCategory.ToString()
            });
        }

        private async Task RunComparisonAsync()
        {
            if (!ValidateInputs())
            {
                return;
            }

            RetrievalMode originalMode = retrievalMode;
            comparisonResults.Clear();
            isGenerating = true;
            statusMessage = "Running retrieval comparison...";
            statusMessageType = MessageType.Info;
            Repaint();

            comparisonResults.Add(new ComparisonResult
            {
                mode = RetrievalMode.None,
                chunks = new List<LoreChunk>()
            });

            retrievalMode = RetrievalMode.Keyword;
            RetrievalResult keywordResult = await RetrieveLoreForCurrentInputsAsync();
            comparisonResults.Add(new ComparisonResult
            {
                mode = RetrievalMode.Keyword,
                chunks = keywordResult.success ? keywordResult.chunks : new List<LoreChunk>(),
                errorMessage = keywordResult.success ? string.Empty : keywordResult.errorMessage
            });

            retrievalMode = RetrievalMode.Semantic;
            RetrievalResult semanticResult = await RetrieveLoreForCurrentInputsAsync();
            comparisonResults.Add(new ComparisonResult
            {
                mode = RetrievalMode.Semantic,
                chunks = semanticResult.success ? semanticResult.chunks : new List<LoreChunk>(),
                errorMessage = semanticResult.success ? string.Empty : semanticResult.errorMessage
            });

            retrievalMode = originalMode;
            isGenerating = false;
            statusMessage = "Comparison complete. Select a mode and generate manually when ready.";
            statusMessageType = MessageType.Info;
            Repaint();
        }

        private void DrawComparisonResults()
        {
            if (comparisonResults.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            foreach (ComparisonResult result in comparisonResults)
            {
                EditorGUILayout.BeginVertical(wrappedBoxStyle);
                EditorGUILayout.LabelField(result.mode.ToString(), EditorStyles.boldLabel);

                if (!string.IsNullOrWhiteSpace(result.errorMessage))
                {
                    EditorGUILayout.LabelField(result.errorMessage, wrappedMiniLabelStyle);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(4f);
                    continue;
                }

                if (result.chunks.Count == 0)
                {
                    EditorGUILayout.LabelField("Retrieved Lore: none", wrappedMiniLabelStyle);
                }
                else
                {
                    for (int i = 0; i < result.chunks.Count; i++)
                    {
                        LoreChunk chunk = result.chunks[i];
                        string scoreLabel = result.mode == RetrievalMode.Semantic
                            ? $"#{i + 1} Similarity: {(chunk.score / 1000f):0.000}"
                            : $"#{i + 1} Score: {chunk.score}";
                        EditorGUILayout.LabelField(scoreLabel, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Source: {chunk.sourceFile}, Chunk: {chunk.chunkIndex}");
                        EditorGUILayout.LabelField(chunk.text, wrappedMiniLabelStyle);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }
        }

        private class RetrievalResult
        {
            public bool success;
            public string errorMessage;
            public List<LoreChunk> chunks = new List<LoreChunk>();

            public static RetrievalResult Success(List<LoreChunk> chunks)
            {
                return new RetrievalResult
                {
                    success = true,
                    chunks = chunks ?? new List<LoreChunk>()
                };
            }

            public static RetrievalResult Failure(string errorMessage)
            {
                return new RetrievalResult
                {
                    success = false,
                    errorMessage = errorMessage
                };
            }
        }

        private class ComparisonResult
        {
            public RetrievalMode mode;
            public List<LoreChunk> chunks = new List<LoreChunk>();
            public string errorMessage;
        }
    }
}
