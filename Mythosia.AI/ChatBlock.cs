﻿using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Mythosia.AI
{
    public enum ActorRole
    {
        [Description("user")] User,
        [Description("system")] System,
        [Description("assistant")] Assistant
    }


    [DebuggerDisplay("Role = {Role}, Content = {Content}")]
    public class Message
    {
        public ActorRole Role { get; set; } = ActorRole.User;
        public string Content { get; set; } = string.Empty;

        public Message(ActorRole role, string content)
        {
            Role = role;
            Content = content;
        }
    }


    public class ChatBlock
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public AIModel Model { get; set; }
        public string SystemMessage { get; set; } = string.Empty;
        public IList<Message> Messages { get; } = new List<Message>();

        public float TopP { get; set; } = 1.0f;
        public float Temperature { get; set; } = 0.7f;
        public float FrequencyPenalty { get; set; } = 0.0f;
        public uint MaxTokens { get; set; } = 1024;
        public bool Stream { get; set; }

        public uint MaxMessageCount { get; set; } = 20;


        public ChatBlock(AIModel model)
        {
            Model = model;
        }

        // MaxMessageCount 초과 시 최신 메시지만 전송
        private IEnumerable<Message> GetLatestMessages()
        {
            return Messages.Skip(Math.Max(0, Messages.Count - (int)MaxMessageCount));
        }

        public object ToChatGptRequestBody()
        {
            var messagesList = new List<object>();

            // Add the system message if it's not empty
            if (!string.IsNullOrEmpty(SystemMessage))
            {
                messagesList.Add(new { role = "system", content = SystemMessage });
            }

            // Add the latest user messages (only the last MaxMessageCount)
            foreach (var message in GetLatestMessages())
            {
                messagesList.Add(new { role = message.Role.ToDescription(), content = message.Content });
            }

            var requestBody = new
            {
                model = Model.ToDescription(),
                messages = messagesList,
                top_p = TopP,
                temperature = Temperature,
                frequency_penalty = FrequencyPenalty,
                max_tokens = MaxTokens,
                stream = Stream
            };

            return requestBody;
        }

        public object ToClaudeRequestBody()
        {
            var messagesList = new List<object>();

            // Add the latest user messages (only the last MaxMessageCount)
            foreach (var message in GetLatestMessages())
            {
                messagesList.Add(new { role = message.Role.ToDescription(), content = message.Content });
            }

            var requestBody = new
            {
                model = Model.ToDescription(),
                system = SystemMessage,
                messages = messagesList,
                top_p = TopP,
                temperature = Temperature,
                stream = Stream,
                max_tokens = MaxTokens
            };

            return requestBody;
        }
    }
}