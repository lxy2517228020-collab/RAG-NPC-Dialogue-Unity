using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RAGNPCDialogue.Editor
{
    public class GeminiEmbeddingService
    {
        private const string GeminiEmbeddingBaseUrl = "https://generativelanguage.googleapis.com/v1beta";

        private readonly string apiKey;
        private readonly string modelName;

        public GeminiEmbeddingService(string apiKey, string modelName)
        {
            this.apiKey = apiKey;
            this.modelName = modelName;
        }

        public async Task<EmbeddingResult> EmbedTextAsync(string text)
        {
            // An embedding turns text into a numeric vector that can be compared with cosine similarity.
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return EmbeddingResult.Failure("Gemini API key is required for Semantic retrieval.");
            }

            if (string.IsNullOrWhiteSpace(modelName))
            {
                return EmbeddingResult.Failure("Embedding model name is required.");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return EmbeddingResult.Failure("Cannot request an embedding for empty text.");
            }

            string normalizedModel = NormalizeModelName(modelName);
            string url = $"{GeminiEmbeddingBaseUrl}/{normalizedModel}:embedContent?key={UnityWebRequest.EscapeURL(apiKey.Trim())}";
            GeminiEmbeddingRequest body = new GeminiEmbeddingRequest
            {
                model = normalizedModel,
                content = new GeminiContent
                {
                    parts = new[]
                    {
                        new GeminiPart { text = text }
                    }
                }
            };

            string json = JsonUtility.ToJson(body);
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 45;
                request.SetRequestHeader("Content-Type", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Delay(50);
                }

                string rawResponse = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    return EmbeddingResult.Failure(BuildErrorMessage(request), rawResponse);
                }

                return ParseEmbeddingResponse(rawResponse);
            }
        }

        private static EmbeddingResult ParseEmbeddingResponse(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return EmbeddingResult.Failure("Gemini embedding API returned an empty response.");
            }

            try
            {
                GeminiEmbeddingResponse response = JsonUtility.FromJson<GeminiEmbeddingResponse>(rawResponse);
                if (response == null || response.embedding == null || response.embedding.values == null || response.embedding.values.Length == 0)
                {
                    return EmbeddingResult.Failure("Gemini embedding response did not contain embedding values.", rawResponse);
                }

                return EmbeddingResult.Success(response.embedding.values, rawResponse);
            }
            catch (Exception exception)
            {
                return EmbeddingResult.Failure($"Failed to parse Gemini embedding response: {exception.Message}", rawResponse);
            }
        }

        private static string NormalizeModelName(string model)
        {
            string trimmed = model.Trim();
            return trimmed.StartsWith("models/", StringComparison.Ordinal) ? trimmed : $"models/{trimmed}";
        }

        private static string BuildErrorMessage(UnityWebRequest request)
        {
            if (request.responseCode == 401 || request.responseCode == 403)
            {
                return $"{request.responseCode} from Gemini embedding API - check your API key and model access.";
            }

            if (request.responseCode == 429)
            {
                return "429 from Gemini embedding API - rate limited or quota exceeded.";
            }

            if (request.responseCode >= 500)
            {
                return $"{request.responseCode} from Gemini embedding API - provider server error.";
            }

            return $"Gemini embedding request failed ({request.responseCode}) - {request.error}";
        }

        public class EmbeddingResult
        {
            public bool success;
            public string errorMessage;
            public string rawResponse;
            public float[] embedding;

            public static EmbeddingResult Success(float[] embedding, string rawResponse)
            {
                return new EmbeddingResult
                {
                    success = true,
                    embedding = embedding,
                    rawResponse = rawResponse
                };
            }

            public static EmbeddingResult Failure(string errorMessage, string rawResponse = "")
            {
                return new EmbeddingResult
                {
                    success = false,
                    errorMessage = errorMessage,
                    rawResponse = rawResponse
                };
            }
        }

        [Serializable]
        private class GeminiEmbeddingRequest
        {
            public string model;
            public GeminiContent content;
        }

        [Serializable]
        private class GeminiContent
        {
            public GeminiPart[] parts;
        }

        [Serializable]
        private class GeminiPart
        {
            public string text;
        }

#pragma warning disable 0649
        [Serializable]
        private class GeminiEmbeddingResponse
        {
            public GeminiEmbedding embedding;
        }

        [Serializable]
        private class GeminiEmbedding
        {
            public float[] values;
        }
#pragma warning restore 0649
    }
}
