using KMchatbot.Models;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KMchatbot
{
    internal static class Converter
    {
        public static ChatMessage ToFunctionResultMessage(this CallToolResult result, string toolCallId, string toolName)
        {
            var json = JsonSerializer.SerializeToElement(result, McpJsonUtilities.DefaultOptions);

            var functionResultContent = new FunctionResultContent(toolCallId, json)
            {
                RawRepresentation = result
            };

            // Optional: extract human-readable text if possible
            string? humanText = null;
            if (result.Content.FirstOrDefault() is TextContentBlock textBlock)
            {
                humanText = textBlock.Text;
            }
            

            var contents = new List<AIContent> { functionResultContent };

            if (!string.IsNullOrEmpty(humanText))
            {
                contents.Add(new TextContent(humanText)); // This will appear in .Text
            }

            var message = new ChatMessage(ChatRole.Assistant, contents)
            {
                AuthorName = toolName,
                MessageId = toolCallId
            };

            return message;
        }

        public static List<ChatMessage> MapStoredToChatMessage(List<StoredChatMessage> storedChatMessages, int conversationId)
        {
            var chatMessages = new List<ChatMessage>();

            foreach (var message in storedChatMessages)
            {
                var content = new TextContent(message.Text ?? string.Empty);
                var contents = new List<AIContent> { content };
                chatMessages.Add(
                    new ChatMessage
                    {
                        Role = ConvertStringToChatRole(message.Role),
                        Contents = contents
                    }
                );
            }

            return chatMessages;

        }

        public static StoredChatMessage MapChatMessageToStored(ChatMessage chatMessage, int conversationId)
        {
            return new StoredChatMessage()
            {
                ConversationId = conversationId,
                Role = chatMessage.Role.ToString(),
                Text = chatMessage.Text,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static ChatRole ConvertStringToChatRole(string roleString)
        {
            return roleString.ToLower() switch
            {
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                "system" => ChatRole.System,
                "tool" => ChatRole.Tool,
                _ => ChatRole.User // default fallback
            };
        }

        

    }   
}

