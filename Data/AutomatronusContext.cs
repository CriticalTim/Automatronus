using Microsoft.EntityFrameworkCore;
using Automatronus.Models;

namespace Automatronus.Data
{
    public class AutomatronusContext : DbContext
    {
        public AutomatronusContext(DbContextOptions<AutomatronusContext> options)
            : base(options)
        {
        }

        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectSkill> ProjectSkills { get; set; }
        public DbSet<ScrapeSession> ScrapeSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Profile>()
                .HasMany(p => p.Skills)
                .WithOne(s => s.Profile)
                .HasForeignKey(s => s.ProfileId);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.RequiredSkills)
                .WithOne(ps => ps.Project)
                .HasForeignKey(ps => ps.ProjectId);

            modelBuilder.Entity<ScrapeSession>()
                .HasMany(s => s.Projects)
                .WithOne(p => p.ScrapeSession)
                .HasForeignKey(p => p.ScrapeSessionId);

            modelBuilder.Entity<Project>()
                .Property(p => p.Budget)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Project>()
                .Property(p => p.Status)
                .HasConversion<string>();
        }
    }
}