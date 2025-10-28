using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using KMchatbot.Data;
using KMchatbot.DTO;
using KMchatbot.Models;
using KMchatbot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.AI;
using Mscc.GenerativeAI;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using ZLinq;

namespace KMchatbot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly McpService _mcpService;
        private readonly IChatClient _chatClient;
        private readonly MessagesDbContext _messagesDbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _config;

        public ChatController(McpService mcpService, IChatClient chatClient, MessagesDbContext messagesDbContext, UserManager<IdentityUser> userManager, IConfiguration config)
        {
            _mcpService = mcpService;
            _chatClient = chatClient;
            _messagesDbContext = messagesDbContext;
            _userManager = userManager;
            _config = config;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            var userId = _userManager.GetUserId(User);
            var response = HttpContext.Response;
            var prompt = request.Prompt;

            var messages = Converter.MapStoredToChatMessage(_messagesDbContext.StoredChatMessages
                .Where(m => m.ConversationId == null || (m.ConversationId == request.ConversationId && (m.Role == "user" || m.IsFinalAssistantReply == 1 || m.IsFinalAssistantReply == 2)))
                .OrderByDescending(m => m.Id)
                .ToList(), request.ConversationId);

            var tools = await _mcpService.ListToolsAsync();

            Microsoft.Extensions.AI.ChatMessage mess = new(ChatRole.User, prompt);
            messages.Add(mess);

            StoredChatMessage storedMess = Converter.MapChatMessageToStored(mess, request.ConversationId);
            storedMess.CreatedAt = DateTime.UtcNow;
            await _messagesDbContext.StoredChatMessages.AddAsync(storedMess);

            while (true)
            {
                var completion = await _chatClient
                    .GetResponseAsync(messages, new()
                    {
                        Tools = [.. tools],
                        Temperature = 0,
                        AllowMultipleToolCalls = true
                    });


                if (completion.FinishReason.Equals(Microsoft.Extensions.AI.ChatFinishReason.ToolCalls) &&
                completion.RawRepresentation is ChatCompletion chatCompletion)
                {
                    mess = new(ChatRole.Assistant, completion.Text);
                    messages.Add(mess);
                    storedMess = Converter.MapChatMessageToStored(mess, request.ConversationId);
                    _messagesDbContext.StoredChatMessages.Add(storedMess);

                    foreach (ChatToolCall toolCall in chatCompletion.ToolCalls)
                    {
                        Microsoft.Extensions.AI.ChatMessage toolResultMessage;

                        switch (toolCall.FunctionName)
                        {
                            case "read_query":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    bool hasQuery = argumentsJson.RootElement.TryGetProperty("query", out JsonElement query);

                                    if (!hasQuery)
                                    {
                                        return BadRequest("The query argument is required.");
                                    }
                                    var result = await _mcpService.CallToolAsync("read_query", new Dictionary<string, object> { ["query"] = query });
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }

                            case "write_query":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    bool hasQuery = argumentsJson.RootElement.TryGetProperty("query", out JsonElement query);

                                    if (!hasQuery)
                                    {
                                        return BadRequest("The query argument is required.");
                                    }
                                    var result = await _mcpService.CallToolAsync("write_query", new Dictionary<string, object> { ["query"] = query });
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "create_table":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    bool hasQuery = argumentsJson.RootElement.TryGetProperty("query", out JsonElement query);

                                    if (!hasQuery)
                                    {
                                        return BadRequest("The query argument is required.");
                                    }
                                    var result = await _mcpService.CallToolAsync("create_table", new Dictionary<string, object> { ["query"] = query });
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "alter_table":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    bool hasQuery = argumentsJson.RootElement.TryGetProperty("query", out JsonElement query);

                                    if (!hasQuery)
                                    {
                                        return BadRequest("The query argument is required.");
                                    }
                                    var result = await _mcpService.CallToolAsync("alter_table", new Dictionary<string, object> { ["query"] = query });
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "list_tables":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    var result = await _mcpService.CallToolAsync("list_tables");
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "describe_table":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    bool hasTableName = argumentsJson.RootElement.TryGetProperty("table_name", out JsonElement table_name);

                                    if (!hasTableName)
                                    {
                                        return BadRequest("The table name argument is required.");
                                    }
                                    var result = await _mcpService.CallToolAsync("describe_table", new Dictionary<string, object> { ["table_name"] = table_name });
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "append_insight":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    bool hasinsight = argumentsJson.RootElement.TryGetProperty("insight", out JsonElement insight);

                                    if (!hasinsight)
                                    {
                                        return BadRequest("The insight argument is required.");
                                    }
                                    var result = await _mcpService.CallToolAsync("alter_table", new Dictionary<string, object> { ["insight"] = insight });
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "list_insights":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);

                                    var result = await _mcpService.CallToolAsync("list_insights");
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            default:
                                {
                                    return BadRequest($"Unknown tool call: {toolCall.FunctionName}");
                                }

                        }
                        if (string.IsNullOrEmpty(toolResultMessage.Text))
                        {
                            toolResultMessage = new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, "Sorry for the problem, I will check again.");
                        }

                        messages.Add(toolResultMessage);

                        var storedToolMsg = Converter.MapChatMessageToStored(toolResultMessage, request.ConversationId);
                        storedToolMsg.IsFinalAssistantReply = 2;
                        _messagesDbContext.StoredChatMessages.Add(storedToolMsg);

                    }
                    await _messagesDbContext.SaveChangesAsync();
                    continue;

                }

                else if (completion.FinishReason == Microsoft.Extensions.AI.ChatFinishReason.Stop)
                {
                    mess = new(ChatRole.Assistant, completion.Text);
                    messages.Add(mess);
                    storedMess = Converter.MapChatMessageToStored(mess, request.ConversationId);
                    storedMess.IsFinalAssistantReply = 1;
                    storedMess.CreatedAt = DateTime.UtcNow;
                    _messagesDbContext.StoredChatMessages.Add(storedMess);

                    await _messagesDbContext.SaveChangesAsync();

                    var text = completion.Text ?? "";
                    int chunkSize = 5;
                    for (int i = 0; i < text.Length; i += chunkSize)
                    {
                        var chunk = text.Substring(i, Math.Min(chunkSize, text.Length - i));
                        var bytes = Encoding.UTF8.GetBytes(chunk + "<|>");
                        await response.BodyWriter.WriteAsync(bytes);
                        await response.BodyWriter.FlushAsync();
                        await Task.Delay(10);
                    }
                    break;
                }

                else
                {
                    return BadRequest("Unknown finish reason.");
                }
            }

            return Ok();
        }

        [Authorize]
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = _userManager.GetUserId(User);
            var conversations = _messagesDbContext.Conversations
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    id = c.ConversationId,
                    title = c.Title,
                    messages = c.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Where(m => m.Role == "user" || (m.Role == "assistant" && m.IsFinalAssistantReply == 1))
                    .Select(m => new
                    {
                        role = m.Role,
                        text = m.Text
                    })
                    .ToList()
                }).ToList();

            return Ok(conversations);
        }

        [Authorize]
        [HttpPost("addConversation")]
        public async Task<IActionResult> Add()
        {
            var userId = _userManager.GetUserId(User);
            Conversation conv = new Conversation();
            conv.Title = "Chat " + (_messagesDbContext.Conversations.Where(c => c.UserId == userId).Count<Conversation>() + 1);
            conv.UserId = userId;
            _messagesDbContext.Add(conv);
            await _messagesDbContext.SaveChangesAsync();
            return Ok(new { id = conv.ConversationId, title = conv.Title, messages = new List<object>() });
        }

    }
}
