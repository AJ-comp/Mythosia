﻿using Mythosia.AI.Exceptions;
using Mythosia.AI.Models;
using Mythosia.AI.Models.Enums;
using Mythosia.AI.Models.Functions;
using Mythosia.AI.Models.Messages;
using Mythosia.AI.Models.Streaming;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia.AI.Services.Google
{
    public partial class GeminiService
    {
        #region Streaming Implementation

        public override async Task StreamCompletionAsync(Message message, Func<string, Task> messageReceivedAsync)
        {
            if (StatelessMode)
            {
                await ProcessStatelessStreamAsync(message, messageReceivedAsync);
                return;
            }

            ActivateChat.Stream = true;
            ActivateChat.Messages.Add(message);

            var request = CreateMessageRequest();
            var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new AIServiceException($"Gemini streaming request failed: {response.ReasonPhrase}", errorContent);
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var allContent = new StringBuilder();
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                    continue;

                var jsonData = line.Substring("data:".Length).Trim();
                if (jsonData == "[DONE]") break;

                try
                {
                    var content = StreamParseJson(jsonData);
                    if (!string.IsNullOrEmpty(content))
                    {
                        allContent.Append(content);
                        await messageReceivedAsync(content);
                    }
                }
                catch (JsonException ex)
                {
                    ActivateChat.Messages.Add(new Message(ActorRole.Assistant, allContent.ToString()));
                    throw new AIServiceException("Failed to parse Gemini streaming response", ex.Message);
                }
            }

            ActivateChat.Messages.Add(new Message(ActorRole.Assistant, allContent.ToString()));
        }

        private async Task ProcessStatelessStreamAsync(Message message, Func<string, Task> messageReceivedAsync)
        {
            var tempChat = new ChatBlock(ActivateChat.Model)
            {
                SystemMessage = ActivateChat.SystemMessage,
                Temperature = ActivateChat.Temperature,
                TopP = ActivateChat.TopP,
                MaxTokens = ActivateChat.MaxTokens,
                Stream = true
            };
            tempChat.Messages.Add(message);

            var backup = ActivateChat;
            ActivateChat = tempChat;

            try
            {
                var request = CreateMessageRequest();
                var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new AIServiceException($"Gemini streaming request failed: {response.ReasonPhrase}", errorContent);
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                        continue;

                    var jsonData = line.Substring("data:".Length).Trim();
                    if (jsonData == "[DONE]") break;

                    try
                    {
                        var content = StreamParseJson(jsonData);
                        if (!string.IsNullOrEmpty(content))
                        {
                            await messageReceivedAsync(content);
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore parse errors in stateless mode
                    }
                }
            }
            finally
            {
                ActivateChat = backup;
            }
        }

        public override async IAsyncEnumerable<StreamingContent> StreamAsync(
            Message message,
            StreamOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Check if functions are available and should be used
            bool useFunctions = options.IncludeFunctionCalls &&
                               ActivateChat.ShouldUseFunctions &&
                               !FunctionsDisabled;

            if (StatelessMode)
            {
                await foreach (var content in ProcessStatelessStreamAsync(
                    message, options, useFunctions, cancellationToken))
                {
                    yield return content;
                }
                yield break;
            }

            // Add message to history and create request
            ActivateChat.Stream = true;
            ActivateChat.Messages.Add(message);

            var request = useFunctions ?
                CreateFunctionMessageRequest() :
                CreateMessageRequest();

            var response = await HttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                yield return new StreamingContent
                {
                    Type = StreamingContentType.Error,
                    Content = null,
                    Metadata = new Dictionary<string, object>
                    {
                        ["error"] = error,
                        ["status_code"] = (int)response.StatusCode
                    }
                };
                yield break;
            }

            // Process streaming response
            await foreach (var content in ProcessGeminiStream(
                response, options, useFunctions, cancellationToken))
            {
                yield return content;
            }
        }

        private async IAsyncEnumerable<StreamingContent> ProcessStatelessStreamAsync(
            Message message,
            StreamOptions options,
            bool useFunctions,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var tempChat = new ChatBlock(ActivateChat.Model)
            {
                SystemMessage = ActivateChat.SystemMessage,
                Temperature = ActivateChat.Temperature,
                TopP = ActivateChat.TopP,
                MaxTokens = ActivateChat.MaxTokens,
                Stream = true,
                Functions = useFunctions ? ActivateChat.Functions : new List<FunctionDefinition>(),
                EnableFunctions = useFunctions
            };
            tempChat.Messages.Add(message);

            var backup = ActivateChat;
            ActivateChat = tempChat;

            try
            {
                var request = useFunctions ?
                    CreateFunctionMessageRequest() :
                    CreateMessageRequest();

                var response = await HttpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    yield return new StreamingContent
                    {
                        Type = StreamingContentType.Error,
                        Content = null,
                        Metadata = new Dictionary<string, object>
                        {
                            ["error"] = error,
                            ["status_code"] = (int)response.StatusCode
                        }
                    };
                    yield break;
                }

                await foreach (var content in ProcessGeminiStream(
                    response, options, useFunctions, cancellationToken))
                {
                    yield return content;
                }
            }
            finally
            {
                ActivateChat = backup;
            }
        }

        private async IAsyncEnumerable<StreamingContent> ProcessGeminiStream(
            HttpResponseMessage response,
            StreamOptions options,
            bool functionsEnabled,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var textBuffer = new StringBuilder();
            var functionCallData = new FunctionCallData();

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                    continue;

                var jsonData = line.Substring("data:".Length).Trim();
                if (jsonData == "[DONE]")
                {
                    if (options.IncludeMetadata)
                    {
                        yield return new StreamingContent
                        {
                            Type = StreamingContentType.Completion,
                            Content = null,
                            Metadata = new Dictionary<string, object>
                            {
                                ["total_length"] = textBuffer.Length
                            }
                        };
                    }
                    break;
                }

                StreamingContent? parsedContent = null;
                try
                {
                    parsedContent = ParseGeminiStreamChunk(
                        jsonData,
                        options,
                        functionCallData);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (parsedContent == null)
                    continue;

                // Handle function calls
                if (parsedContent.Type == StreamingContentType.FunctionCall)
                {
                    if (functionsEnabled && functionCallData.IsComplete && functionCallData.Name != null)
                    {
                        // Execute function
                        var functionResult = await ExecuteFunctionCallAsync(
                            functionCallData,
                            options,
                            cancellationToken);

                        yield return functionResult;

                        // Add function result to messages
                        ActivateChat.Messages.Add(new Message(ActorRole.Function,
                            functionResult.Metadata?["result"]?.ToString() ?? "")
                        {
                            Metadata = new Dictionary<string, object>
                            {
                                ["function_name"] = functionCallData.Name
                            }
                        });

                        functionCallData = new FunctionCallData();

                        // Request completion based on function result
                        await foreach (var responseContent in StreamAsync(
                            new Message(ActorRole.User, "Please provide a response based on the function result."),
                            options,
                            cancellationToken))
                        {
                            yield return responseContent;
                        }

                        yield break;
                    }
                    else if (options.IncludeMetadata)
                    {
                        yield return parsedContent;
                    }
                }
                else if (parsedContent.Type == StreamingContentType.Text)
                {
                    textBuffer.Append(parsedContent.Content);

                    if (!options.TextOnly || parsedContent.Content != null)
                    {
                        yield return parsedContent;
                    }
                }
                else if (options.IncludeMetadata)
                {
                    yield return parsedContent;
                }
            }

            if (textBuffer.Length > 0)
            {
                ActivateChat.Messages.Add(new Message(ActorRole.Assistant, textBuffer.ToString()));
            }
        }

        private async Task<StreamingContent> ExecuteFunctionCallAsync(
            FunctionCallData functionCallData,
            StreamOptions options,
            CancellationToken cancellationToken)
        {
            var content = new StreamingContent
            {
                Type = StreamingContentType.FunctionResult
            };

            try
            {
                var arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    functionCallData.Arguments.ToString()) ?? new Dictionary<string, object>();

                var result = await ProcessFunctionCallAsync(
                    functionCallData.Name ?? "",
                    arguments);

                if (options.IncludeMetadata)
                {
                    content.Metadata = new Dictionary<string, object>
                    {
                        ["function_calling"] = false,
                        ["function_name"] = functionCallData.Name ?? "",
                        ["status"] = "completed",
                        ["result"] = result
                    };
                }
            }
            catch (Exception ex)
            {
                content.Type = StreamingContentType.Error;
                content.Metadata = new Dictionary<string, object>
                {
                    ["function_calling"] = false,
                    ["function_name"] = functionCallData.Name ?? "",
                    ["status"] = "error",
                    ["error"] = ex.Message
                };
            }

            return content;
        }

        #endregion
    }
}