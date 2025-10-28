using KMchatbot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace KMchatbot.Data
{
    public class MessagesDbContext : IdentityDbContext<IdentityUser>
    {
        public MessagesDbContext(DbContextOptions<MessagesDbContext> options)
            : base(options)
        {
        }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<StoredChatMessage> StoredChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<StoredChatMessage>()
                .HasKey(x => x.Id);
            modelBuilder.Entity<StoredChatMessage>()
               .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<StoredChatMessage>()
                .Property(m => m.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<StoredChatMessage>()
                .Property(e => e.Text);
            modelBuilder.Entity<StoredChatMessage>()
                .HasData(
                    
                );

            modelBuilder.Entity<StoredChatMessage>()
                .Property(s => s.IsFinalAssistantReply);
            modelBuilder.Entity<StoredChatMessage>()
                .Property(s => s.IsFinalAssistantReply);

            modelBuilder.Entity<Conversation>()
                .Property(c => c.Title);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
             
        }
    }
}
