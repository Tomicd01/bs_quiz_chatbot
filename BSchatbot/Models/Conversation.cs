using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.AI;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KMchatbot.Models
{
    public class Conversation
    {
        [Key]
        public int ConversationId { get; set; }
        public string Title { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }
        public ICollection<StoredChatMessage> Messages { get; set; } = new List<StoredChatMessage>();
    }
}
