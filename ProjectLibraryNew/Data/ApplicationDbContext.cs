using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.Models;

namespace ProjectLibrary.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Analysis> Analyses { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<BookCollection> BookCollections { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<StudyEvent> StudyEvents { get; set; }

        // НОВАТА ТАБЛИЦА ЗА ПРОГРЕСА:
        public DbSet<UserBookProgress> UserBookProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Book>()
                .HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId);

            builder.Entity<Analysis>()
                .HasOne(a => a.Book)
                .WithMany(b => b.Analyses)
                .HasForeignKey(a => a.BookId);

            builder.Entity<Test>()
                .HasOne(t => t.Book)
                .WithMany(b => b.Tests)
                .HasForeignKey(t => t.BookId);

            builder.Entity<TestResult>()
                .HasOne(tr => tr.Test)
                .WithMany(t => t.TestResults)
                .HasForeignKey(tr => tr.TestId);

            builder.Entity<TestResult>()
                .HasOne(tr => tr.User)
                .WithMany()
                .HasForeignKey(tr => tr.UserId);

            builder.Entity<Comment>()
                .HasOne(c => c.Book)
                .WithMany(b => b.Comments)
                .HasForeignKey(c => c.BookId);

            // ФИКС: Сменихме Cascade с Restrict, за да избегнем циклична грешка в SQL Server
            builder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BookCollection>()
                .HasMany(bc => bc.Books)
                .WithMany(b => b.BookCollections)
                .UsingEntity(j => j.ToTable("BookCollectionBooks"));

            builder.Entity<TestResult>(entity =>
            {
                entity.Property(e => e.ResultDetailsJson)
                    .HasColumnType("nvarchar(max)");
            });

            builder.Entity<Test>(entity =>
            {
                entity.Property(e => e.QuestionsJson)
                    .HasColumnType("nvarchar(max)");
            });
        }
    }
}