using Microsoft.Extensions.AI;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KMchatbot.Models
{
    public class StoredChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int? ConversationId { get; set; } // Foreign key
        public Conversation Conversation { get; set; }

        public string Role { get; set; } // "user" or "assistant"
        public string Text { get; set; }
        public int IsFinalAssistantReply { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = new DateTime(2001, 1, 1, 12, 0, 0);
    }
}
