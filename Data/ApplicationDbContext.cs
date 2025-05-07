using Microsoft.EntityFrameworkCore;
using SkillSwap.Models;

namespace SkillSwap.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Request> Requests { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure table names to match SQL schema
            modelBuilder.Entity<ApplicationUser>().ToTable("User");
            modelBuilder.Entity<Post>().ToTable("Posts");
            modelBuilder.Entity<Request>().ToTable("Request");
            
            // Configure nullable string properties
            modelBuilder.Entity<Post>()
                .Property(p => p.Description).IsRequired(false);
            modelBuilder.Entity<Post>()
                .Property(p => p.Image).IsRequired(false);
            
            // Configure relationships
            modelBuilder.Entity<Post>()
                .HasOne(p => p.User)
                .WithMany(p => p.Posts)
                .HasForeignKey(p => p.User_id);
                
            modelBuilder.Entity<Request>()
                .HasOne(r => r.User)
                .WithMany(u => u.Requests)
                .HasForeignKey(r => r.User_id);
                
            modelBuilder.Entity<Request>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Requests)
                .HasForeignKey(r => r.Post_id);
        }
    }
}