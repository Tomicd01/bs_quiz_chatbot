namespace KMchatbot.DTO
{
    public class ChatRequest
    {
        public string Prompt { get; set; }
        public int ConversationId { get; set; } = 1;
    }
}
