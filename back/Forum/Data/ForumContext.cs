using Thread = Forum.Models.Thread;
using Reply = Forum.Models.Reply;
using Category = Forum.Models.Category;
using Microsoft.EntityFrameworkCore;
using Forum.Models;

namespace Forum.Data
{
    public class ForumContext : DbContext
    {
        public ForumContext(DbContextOptions<ForumContext> options) : base(options) { }

        public DbSet<Forum.Models.Thread> Threads => Set<Thread>();
        public DbSet<Forum.Models.Reply> Replies => Set<Reply>();
        public DbSet<Forum.Models.Category> Categories => Set<Category>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // CONFIGURAR NOMBRES EXACTOS DE TABLAS (como están en Supabase)
            modelBuilder.Entity<Thread>().ToTable("threads");
            modelBuilder.Entity<Reply>().ToTable("replies");
            modelBuilder.Entity<Category>().ToTable("categories");

            // Thread configuration
            modelBuilder.Entity<Thread>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Content).IsRequired();
                
                entity.HasOne(t => t.Category)
                      .WithMany(c => c.Threads)
                      .HasForeignKey(t => t.CategoryId);

                entity.HasMany(t => t.Replies)
                      .WithOne(r => r.Thread)
                      .HasForeignKey(r => r.ThreadId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(t => t.Tags)
                      .HasConversion(
                          v => string.Join(',', v),
                          v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                      );
            });

            // Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(c => c.Name).IsUnique();
            });

            // Seed data - COMENTADO porque ya existen en Supabase
            /*
            modelBuilder.Entity<Category>().HasData(
                new Category { 
                    Id = Guid.NewGuid(), 
                    Name = "General", 
                    Description = "Temas generales de discusión",
                    Icon = "fas fa-comments",
                    Color = "#3B82F6",
                    CreatedAt = DateTime.UtcNow
                },
                new Category { 
                    Id = Guid.NewGuid(), 
                    Name = "Tareas y deberes", 
                    Description = "Discusiones sobre tareas escolares",
                    Icon = "fas fa-book",
                    Color = "#10B981",
                    CreatedAt = DateTime.UtcNow
                },
                new Category { 
                    Id = Guid.NewGuid(), 
                    Name = "Eventos escolares", 
                    Description = "Eventos y actividades de la escuela",
                    Icon = "fas fa-calendar",
                    Color = "#F59E0B",
                    CreatedAt = DateTime.UtcNow
                },
                new Category { 
                    Id = Guid.NewGuid(), 
                    Name = "Rendimiento académico", 
                    Description = "Seguimiento del progreso académico",
                    Icon = "fas fa-chart-line",
                    Color = "#EF4444",
                    CreatedAt = DateTime.UtcNow
                },
                new Category { 
                    Id = Guid.NewGuid(), 
                    Name = "Comportamiento", 
                    Description = "Temas relacionados con conducta y disciplina",
                    Icon = "fas fa-users",
                    Color = "#8B5CF6",
                    CreatedAt = DateTime.UtcNow
                }
            );
            */
        }
    }
}
