﻿using Mythosia.AI.Exceptions;
using Mythosia.AI.Models;
using Mythosia.AI.Models.Enums;
using Mythosia.AI.Models.Functions;
using Mythosia.AI.Models.Messages;
using Mythosia.AI.Models.Streaming;
using Mythosia.AI.Services.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TiktokenSharp;

namespace Mythosia.AI.Services.OpenAI
{
    public partial class ChatGptService : AIService
    {
        public override AIProvider Provider => AIProvider.OpenAI;

        public ChatGptService(string apiKey, HttpClient httpClient)
            : base(apiKey, "https://api.openai.com/v1/", httpClient)
        {
            var chatBlock = new ChatBlock(AIModel.Gpt4_1)
            {
                MaxTokens = 16000
            };
            AddNewChat(chatBlock);
        }

        #region Core Completion Methods

        public override async Task<string> GetCompletionAsync(Message message)
        {
            var policy = CurrentPolicy ?? DefaultPolicy;
            CurrentPolicy = null;

            using var cts = policy.TimeoutSeconds.HasValue
                ? new CancellationTokenSource(TimeSpan.FromSeconds(policy.TimeoutSeconds.Value))
                : new CancellationTokenSource();

            // Stateless mode handling
            ChatBlock originalChat = null;
            if (StatelessMode)
            {
                originalChat = ActivateChat;
                ActivateChat = ActivateChat.CloneWithoutMessages();
            }

            try
            {
                ActivateChat.Stream = false;
                ActivateChat.Messages.Add(message);

                // Main loop for function calling
                for (int round = 0; round < policy.MaxRounds; round++)
                {
                    var result = await ProcessSingleRoundAsync(round, policy, cts.Token);
                    if (result.IsComplete)
                        return result.Content;
                }

                throw new AIServiceException($"Maximum rounds ({policy.MaxRounds}) exceeded");
            }
            catch (OperationCanceledException)
            {
                throw new AIServiceException($"Request timeout after {policy.TimeoutSeconds} seconds");
            }
            finally
            {
                if (originalChat != null)
                {
                    ActivateChat = originalChat;
                }
            }
        }

        /// <summary>
        /// Process a single round of API interaction
        /// </summary>
        private async Task<RoundResult> ProcessSingleRoundAsync(
            int round,
            FunctionCallingPolicy policy,
            CancellationToken cancellationToken)
        {
            if (policy.EnableLogging)
            {
                Console.WriteLine($"[Round {round + 1}/{policy.MaxRounds}]");
            }

            // 1. Send API request
            var response = await SendApiRequestAsync(cancellationToken);

            // 2. Process response
            var responseContent = await response.Content.ReadAsStringAsync();

            // 3. Handle based on function support
            bool useFunctions = ActivateChat.Functions?.Count > 0
                               && ActivateChat.EnableFunctions
                               && !FunctionsDisabled;

            if (useFunctions)
            {
                return await ProcessFunctionResponseAsync(responseContent, policy);
            }
            else
            {
                return ProcessRegularResponseAsync(responseContent);
            }
        }

        /// <summary>
        /// Send API request
        /// </summary>
        private async Task<HttpResponseMessage> SendApiRequestAsync(CancellationToken cancellationToken)
        {
            bool useFunctions = ActivateChat.Functions?.Count > 0
                               && ActivateChat.EnableFunctions
                               && !FunctionsDisabled;

            var request = useFunctions
                ? CreateFunctionMessageRequest()
                : CreateMessageRequest();

            var response = await HttpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new AIServiceException($"API request failed: {response.ReasonPhrase}", errorContent);
            }

            return response;
        }

        /// <summary>
        /// Process response with function calling
        /// </summary>
        private async Task<RoundResult> ProcessFunctionResponseAsync(
            string responseContent,
            FunctionCallingPolicy policy)
        {
            var (content, functionCall) = ExtractFunctionCall(responseContent);

            // Function call detected
            if (functionCall != null)
            {
                if (policy.EnableLogging)
                {
                    Console.WriteLine($"  Executing function: {functionCall.Name}");
                }

                await ExecuteFunctionAsync(functionCall);

                // Continue to next round
                return RoundResult.Continue();
            }

            // Final response received
            if (!string.IsNullOrEmpty(content))
            {
                ActivateChat.Messages.Add(new Message(ActorRole.Assistant, content));
                return RoundResult.Complete(content);
            }

            // Empty response, try next round
            return RoundResult.Continue();
        }

        /// <summary>
        /// Process regular response (no functions)
        /// </summary>
        private RoundResult ProcessRegularResponseAsync(string responseContent)
        {
            var result = ExtractResponseContent(responseContent);

            if (!string.IsNullOrEmpty(result))
            {
                ActivateChat.Messages.Add(new Message(ActorRole.Assistant, result));
                return RoundResult.Complete(result);
            }

            return RoundResult.Continue();
        }

        /// <summary>
        /// Execute function and save results
        /// </summary>
        private async Task ExecuteFunctionAsync(FunctionCall functionCall)
        {
            // 1. Save function call message
            var functionCallMessage = new Message(ActorRole.Assistant, string.Empty)
            {
                Metadata = new Dictionary<string, object>
                {
                    [MessageMetadataKeys.MessageType] = "function_call",
                    [MessageMetadataKeys.FunctionId] = functionCall.Id,
                    [MessageMetadataKeys.FunctionSource] = functionCall.Source,
                    [MessageMetadataKeys.FunctionName] = functionCall.Name,
                    [MessageMetadataKeys.FunctionArguments] = JsonSerializer.Serialize(functionCall.Arguments),
                    ["model"] = ActivateChat.Model
                }
            };

            ActivateChat.Messages.Add(functionCallMessage);

            // 2. Execute function
            var result = await ProcessFunctionCallAsync(functionCall.Name, functionCall.Arguments);

            if (string.IsNullOrEmpty(result))
            {
                Console.WriteLine($"[WARNING] Function {functionCall.Name} returned empty result");
                result = "Function executed successfully";
            }

            // 3. Save function result
            var metadata = new Dictionary<string, object>
            {
                [MessageMetadataKeys.MessageType] = "function_result",
                [MessageMetadataKeys.FunctionId] = functionCall.Id,
                [MessageMetadataKeys.FunctionSource] = functionCall.Source,
                [MessageMetadataKeys.FunctionName] = functionCall.Name,
                ["model"] = ActivateChat.Model
            };

            ActivateChat.Messages.Add(new Message(ActorRole.Function, result)
            {
                Metadata = metadata
            });
        }

        #endregion

        #region Request Creation

        protected override HttpRequestMessage CreateMessageRequest()
        {
            var requestBody = BuildRequestBody();
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // Determine endpoint based on model
            string endpoint = IsNewApiModel(ActivateChat.Model)
                ? (ActivateChat.Stream ? "responses?stream=true" : "responses")
                : "chat/completions";

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };

            request.Headers.Add("Authorization", $"Bearer {ApiKey}");
            return request;
        }

        #endregion

        #region Token Counting

        public override async Task<uint> GetInputTokenCountAsync()
        {
            var encoding = TikToken.EncodingForModel("gpt-4o");

            var allMessagesBuilder = new StringBuilder();

            // Add system message
            if (!string.IsNullOrEmpty(ActivateChat.SystemMessage))
            {
                allMessagesBuilder.Append(ActivateChat.SystemMessage).Append('\n');
            }

            // Add all messages
            foreach (var message in ActivateChat.GetLatestMessages())
            {
                if (message.HasMultimodalContent)
                {
                    foreach (var content in message.Contents)
                    {
                        if (content is TextContent textContent)
                        {
                            allMessagesBuilder.Append(textContent.Text).Append('\n');
                        }
                        else if (content is ImageContent)
                        {
                            // Images consume fixed tokens based on detail level
                            allMessagesBuilder.Append("[IMAGE]").Append('\n');
                        }
                    }
                }
                else
                {
                    allMessagesBuilder.Append(message.Role).Append('\n')
                                      .Append(message.Content).Append('\n');
                }
            }

            var textTokens = (uint)encoding.Encode(allMessagesBuilder.ToString()).Count;

            // Add image tokens
            var imageTokens = ActivateChat.Messages
                .SelectMany(m => m.Contents)
                .OfType<ImageContent>()
                .Sum(img => img.EstimateTokens());

            return await Task.FromResult(textTokens + (uint)imageTokens);
        }

        public override async Task<uint> GetInputTokenCountAsync(string prompt)
        {
            var encoding = TikToken.EncodingForModel("gpt-4o");
            return await Task.FromResult((uint)encoding.Encode(prompt).Count);
        }

        #endregion

        #region Vision Support

        public override async Task<string> GetCompletionWithImageAsync(string prompt, string imagePath)
        {
            var currentModel = ActivateChat.Model;

            // Check if current model supports vision
            bool supportsVision = currentModel.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase) ||
                                 currentModel.Contains("gpt-4o") ||
                                 currentModel.Contains("gpt-4-turbo") ||
                                 currentModel.Contains("vision");

            if (!supportsVision)
            {
                // Switch to a vision-capable model
                if (currentModel.Contains("mini"))
                {
                    // If using mini model, switch to full gpt-4o
                    ActivateChat.ChangeModel(AIModel.Gpt4oLatest);
                }
                else
                {
                    // For other models, switch to gpt-4o
                    ActivateChat.ChangeModel(AIModel.Gpt4oLatest);
                }

                Console.WriteLine($"[GetCompletionWithImageAsync] Switched from {currentModel} to {ActivateChat.Model} for vision support");
            }

            return await base.GetCompletionWithImageAsync(prompt, imagePath);
        }

        #endregion

        #region OpenAI-Specific Features

        /// <summary>
        /// Fine-tunes the response with specific OpenAI parameters
        /// </summary>
        public ChatGptService WithOpenAIParameters(float? presencePenalty = null, float? frequencyPenalty = null, int? bestOf = null)
        {
            if (presencePenalty.HasValue)
            {
                ActivateChat.PresencePenalty = presencePenalty.Value;
            }
            if (frequencyPenalty.HasValue)
            {
                ActivateChat.FrequencyPenalty = frequencyPenalty.Value;
            }
            return this;
        }

        /// <summary>
        /// Sets GPT-5 specific parameters
        /// </summary>
        public ChatGptService WithGpt5Parameters(string reasoningEffort = "medium", string verbosity = "medium")
        {
            if (ActivateChat.Model.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[GPT-5 Config] Reasoning: {reasoningEffort}, Verbosity: {verbosity}");
            }
            return this;
        }

        #endregion
    }
}