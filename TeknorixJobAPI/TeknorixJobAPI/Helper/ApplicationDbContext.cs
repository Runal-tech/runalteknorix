using Microsoft.EntityFrameworkCore;
using TeknorixJobAPI.Models;

namespace TeknorixJobAPI.Helper
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique constraint for Job Code
            modelBuilder.Entity<Job>()
                .HasIndex(j => j.Code)
                .IsUnique();

            // Configure unique constraint for Department Title
            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Title)
                .IsUnique();
        }
    }
}