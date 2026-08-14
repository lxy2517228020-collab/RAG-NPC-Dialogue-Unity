using UnityEditor;

namespace RAGNPCDialogue.Editor
{
    public static class ApiKeyProvider
    {
        private const string ApiKeyEditorPrefsKey = "RAGNPCDialogue.OpenAICompatible.ApiKey";
        private const string BaseUrlEditorPrefsKey = "RAGNPCDialogue.OpenAICompatible.BaseUrl";
        private const string ModelEditorPrefsKey = "RAGNPCDialogue.OpenAICompatible.Model";

        public static string GetApiKey()
        {
            string editorPrefsKey = EditorPrefs.GetString(ApiKeyEditorPrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(editorPrefsKey))
            {
                return editorPrefsKey;
            }

            string environmentKey = System.Environment.GetEnvironmentVariable("OPENAI_COMPATIBLE_API_KEY");
            if (!string.IsNullOrWhiteSpace(environmentKey))
            {
                return environmentKey;
            }

            return System.Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        }

        public static void SaveApiKey(string apiKey)
        {
            EditorPrefs.SetString(ApiKeyEditorPrefsKey, apiKey ?? string.Empty);
        }

        public static string GetBaseUrl()
        {
            return EditorPrefs.GetString(BaseUrlEditorPrefsKey, string.Empty);
        }

        public static void SaveBaseUrl(string baseUrl)
        {
            EditorPrefs.SetString(BaseUrlEditorPrefsKey, baseUrl ?? string.Empty);
        }

        public static string GetModel()
        {
            return EditorPrefs.GetString(ModelEditorPrefsKey, string.Empty);
        }

        public static void SaveModel(string model)
        {
            EditorPrefs.SetString(ModelEditorPrefsKey, model ?? string.Empty);
        }
    }
}
