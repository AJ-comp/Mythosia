﻿using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Mythosia.AI
{
    public class ClaudeService : AIService
    {
        public ClaudeService(string apiKey, HttpClient httpClient) : base(apiKey, "https://api.anthropic.com/v1/", httpClient)
        {
            Model = AIModel.Claude3_5Sonnet;
            MaxTokens = 8192;
        }


        protected override string StreamParseJson(string jsonData)
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);
            if (jsonElement.TryGetProperty("delta", out JsonElement deltaElement) &&
                deltaElement.TryGetProperty("text", out JsonElement textElement))
            {
                return textElement.GetString();
            }

            return string.Empty;
        }

        protected override HttpRequestMessage CreateRequest(string prompt, bool isStream)
        {
            // 요청 바디 생성
            var requestBody = new
            {
                model = GetModelString(),
                messages = new[]
                {
                new { role = "user", content = prompt }
                },
                temperature = Temperature,
                stream = isStream,
                max_tokens = MaxTokens
            };

            // HttpRequestMessage를 생성하고 메서드와 엔드포인트 설정
            var request = new HttpRequestMessage(HttpMethod.Post, "messages")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            // 헤더 추가
            request.Headers.Add("x-api-key", ApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            return request;
        }


        protected override string ExtractResponseContent(string responseContent)
        {
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return responseObj.GetProperty("content")[0].GetProperty("text").GetString();
        }
    }

}