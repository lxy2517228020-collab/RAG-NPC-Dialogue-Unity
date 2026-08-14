using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RAGNPCDialogue.Editor
{
    public class OpenAICompatibleLLMClient : ILLMClient
    {
        private readonly string baseUrl;
        private readonly string apiKey;
        private readonly string modelName;
        private readonly float temperature;

        public OpenAICompatibleLLMClient(
            string baseUrl,
            string apiKey,
            string modelName,
            float temperature = 0.7f)
        {
            this.baseUrl = baseUrl;
            this.apiKey = apiKey;
            this.modelName = modelName;
            this.temperature = temperature;
        }

        public async Task<LLMGenerationResult> GenerateDialogueAsync(
            NPCProfile profile,
            DialogueCategory category,
            string scenario,
            string prompt)
        {
            string validationError = ValidateConfiguration();
            if (!string.IsNullOrEmpty(validationError))
            {
                return LLMGenerationResult.Failure(validationError);
            }

            ChatCompletionRequest requestBody = new ChatCompletionRequest
            {
                model = modelName.Trim(),
                temperature = temperature,
                messages = new[]
                {
                    new ChatMessage
                    {
                        role = "system",
                        content = "You are a professional video game dialogue writer."
                    },
                    new ChatMessage
                    {
                        role = "user",
                        content = prompt
                    }
                }
            };

            string requestJson = JsonUtility.ToJson(requestBody);
            byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);

            using (UnityWebRequest request = new UnityWebRequest(BuildChatCompletionsUrl(), UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(requestBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 45;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey.Trim()}");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Delay(50);
                }

                string rawResponse = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    return LLMGenerationResult.Failure(BuildHttpErrorMessage(request), rawResponse);
                }

                return ParseChatCompletionResponse(rawResponse);
            }
        }

        private string ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return "Base URL is required for Real API mode.";
            }

            if (string.IsNullOrWhiteSpace(modelName))
            {
                return "Model name is required for Real API mode.";
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "API key is required for Real API mode. Save it in the Editor UI or set OPENAI_COMPATIBLE_API_KEY.";
            }

            return string.Empty;
        }

        private string BuildChatCompletionsUrl()
        {
            string trimmedBaseUrl = baseUrl.Trim().TrimEnd('/');
            if (trimmedBaseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedBaseUrl;
            }

            return $"{trimmedBaseUrl}/chat/completions";
        }

        private string BuildHttpErrorMessage(UnityWebRequest request)
        {
            long statusCode = request.responseCode;

            if (statusCode == 400)
            {
                return "400 Bad Request - check the Base URL, model name, and request format.";
            }

            if (statusCode == 401)
            {
                return "401 Unauthorized - check your API key.";
            }

            if (statusCode == 403)
            {
                return "403 Forbidden - this key may not have access to the selected model.";
            }

            if (statusCode == 429)
            {
                return "429 Rate Limited - wait and try again, or check provider quota.";
            }

            if (statusCode >= 500)
            {
                return $"{statusCode} Server Error - the provider returned an internal error.";
            }

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return $"Connection error - {request.error}";
            }

            if (request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return $"Response processing error - {request.error}";
            }

            return $"HTTP request failed ({statusCode}) - {request.error}";
        }

        private LLMGenerationResult ParseChatCompletionResponse(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return LLMGenerationResult.Failure("Provider returned an empty response.");
            }

            ChatCompletionResponse response;
            try
            {
                response = JsonUtility.FromJson<ChatCompletionResponse>(rawResponse);
            }
            catch (Exception exception)
            {
                return LLMGenerationResult.Failure($"Invalid chat completion JSON response: {exception.Message}", rawResponse);
            }

            if (response == null || response.choices == null || response.choices.Length == 0)
            {
                return LLMGenerationResult.Failure("Provider response did not include any choices.", rawResponse);
            }

            string content = response.choices[0].message != null ? response.choices[0].message.content : string.Empty;
            if (string.IsNullOrWhiteSpace(content))
            {
                return LLMGenerationResult.Failure("Provider response did not include message content.", rawResponse);
            }

            return ParseStructuredDialogue(content, rawResponse);
        }

        private LLMGenerationResult ParseStructuredDialogue(string content, string rawResponse)
        {
            string json = StripMarkdownCodeFence(content);

            LLMDialogueResponse response;
            try
            {
                response = JsonUtility.FromJson<LLMDialogueResponse>(json);
            }
            catch (Exception exception)
            {
                return LLMGenerationResult.Failure($"Malformed structured dialogue JSON: {exception.Message}", rawResponse);
            }

            if (response == null || response.lines == null || response.lines.Length == 0)
            {
                return LLMGenerationResult.Failure("Structured response is missing a non-empty lines array.", rawResponse);
            }

            if (response.lines.Length != 3)
            {
                return LLMGenerationResult.Failure("Structured response must contain exactly 3 dialogue lines.", rawResponse);
            }

            List<DialogueLine> dialogueLines = new List<DialogueLine>();
            foreach (LLMDialogueLine generatedLine in response.lines)
            {
                if (generatedLine == null || string.IsNullOrWhiteSpace(generatedLine.text))
                {
                    return LLMGenerationResult.Failure("Structured response contains a dialogue line with missing text.", rawResponse);
                }

                dialogueLines.Add(new DialogueLine
                {
                    speaker = string.IsNullOrWhiteSpace(generatedLine.speaker) ? "NPC" : generatedLine.speaker.Trim(),
                    text = generatedLine.text.Trim(),
                    emotionOrTag = string.IsNullOrWhiteSpace(generatedLine.emotion) ? "neutral" : generatedLine.emotion.Trim()
                });
            }

            return LLMGenerationResult.Success(dialogueLines, $"OpenAICompatible:{modelName.Trim()}", rawResponse);
        }

        private static string StripMarkdownCodeFence(string content)
        {
            string trimmed = content.Trim();
            if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                return trimmed;
            }

            int firstNewLine = trimmed.IndexOf('\n');
            int lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);

            if (firstNewLine < 0 || lastFence <= firstNewLine)
            {
                return trimmed;
            }

            return trimmed.Substring(firstNewLine + 1, lastFence - firstNewLine - 1).Trim();
        }

        [Serializable]
        private class ChatCompletionRequest
        {
            public string model;
            public ChatMessage[] messages;
            public float temperature;
        }

        [Serializable]
        private class ChatMessage
        {
            public string role;
            public string content;
        }

#pragma warning disable 0649
        [Serializable]
        private class ChatCompletionResponse
        {
            public ChatChoice[] choices;
        }

        [Serializable]
        private class ChatChoice
        {
            public ChatMessage message;
        }

        [Serializable]
        private class LLMDialogueResponse
        {
            public LLMDialogueLine[] lines;
        }

        [Serializable]
        private class LLMDialogueLine
        {
            public string speaker;
            public string text;
            public string emotion;
        }
#pragma warning restore 0649
    }
}
