﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TiktokenSharp;

namespace Mythosia.AI
{
    public class DeepSeekService : AIService
    {
        public DeepSeekService(string apiKey, HttpClient httpClient)
            : base(apiKey, "https://api.deepseek.com/v1/", httpClient)
        {
            var chatBlock = new ChatBlock(AIModel.DeepSeekChat);
            chatBlock.MaxTokens = 8000; // DeepSeek 기본 최대 토큰
            AddNewChat(chatBlock);
        }

        protected override string StreamParseJson(string jsonData)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(jsonData);
                var root = jsonDoc.RootElement;
                return root
                    .GetProperty("choices")[0]
                    .GetProperty("delta")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        protected override HttpRequestMessage CreateMessageRequest()
        {
            // 1. Build a messages list, starting with system message (if it's not empty).
            var messageList = new List<object>();

            if (!string.IsNullOrEmpty(ActivateChat.SystemMessage))
            {
                messageList.Add(new
                {
                    role = "system",
                    content = ActivateChat.SystemMessage
                });
            }

            // 2. Append the latest user/assistant messages
            messageList.AddRange(
                ActivateChat.GetLatestMessages().Select(m => new
                {
                    role = m.Role.ToDescription(),
                    content = m.Content
                })
            );

            var requestBody = new
            {
                model = ActivateChat.Model,
                messages = messageList,
                temperature = ActivateChat.Temperature,
                max_tokens = ActivateChat.MaxTokens,
                stream = ActivateChat.Stream
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {ApiKey}");
            request.Headers.Add("Accept", "application/json");

            return request;
        }

        protected override string ExtractResponseContent(string responseContent)
        {
            using var jsonDoc = JsonDocument.Parse(responseContent);
            return jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }

        public override async Task<uint> GetInputTokenCountAsync()
        {
            var encoding = TikToken.EncodingForModel("gpt-4o");

            var allMessagesBuilder = new StringBuilder(ActivateChat.SystemMessage).Append('\n');
            foreach (var message in ActivateChat.GetLatestMessages())
            {
                allMessagesBuilder.Append(message.Role).Append('\n')
                                  .Append(message.Content).Append('\n');
            }

            var allMessages = allMessagesBuilder.ToString();
            return (uint)encoding.Encode(allMessages).Count;
        }

        public async override Task<uint> GetInputTokenCountAsync(string prompt)
        {
            var encoding = TikToken.EncodingForModel("gpt-4o");

            return (uint)encoding.Encode(prompt).Count;
        }


        // DeepSeek은 이미지 생성 미지원이므로 예외 처리
        public override Task<byte[]> GenerateImageAsync(string prompt, string size = "1024x1024")
            => throw new NotSupportedException("DeepSeek does not support image generation");

        public override Task<string> GenerateImageUrlAsync(string prompt, string size = "1024x1024")
            => throw new NotSupportedException("DeepSeek does not support image generation");
    }
}