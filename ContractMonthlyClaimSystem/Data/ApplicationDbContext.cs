using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MonthlyClaim> MonthlyClaims { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure decimal precision
            builder.Entity<MonthlyClaim>()
                .Property(m => m.HoursWorked)
                .HasPrecision(10, 2);

            builder.Entity<MonthlyClaim>()
                .Property(m => m.HourlyRate)
                .HasPrecision(10, 2);

            // Configure relationships
            builder.Entity<MonthlyClaim>()
                .HasOne(c => c.Lecturer)
                .WithMany(u => u.Claims)
                .HasForeignKey(c => c.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}