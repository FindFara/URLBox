using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using URLBox.Domain.Entities;

namespace URLBox.Infrastructure.Persistance
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Url> Urls { get; set; }
        public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Project>()
                .HasMany(p => p.Roles)
                .WithMany(r => r.Projects)
                .UsingEntity<Dictionary<string, object>>(
                    "ProjectRole",
                    j => j.HasOne<ApplicationRole>()
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .HasPrincipalKey(r => r.Id)
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Project>()
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .HasPrincipalKey(p => p.Id)
                        .OnDelete(DeleteBehavior.Cascade));

            builder.Entity<Project>()
                .HasMany(p => p.Urls)
                .WithMany(u => u.Projects)
                .UsingEntity<Dictionary<string, object>>(
                    "ProjectUrl",
                    j => j.HasOne<Url>()
                        .WithMany()
                        .HasForeignKey("UrlId")
                        .HasPrincipalKey(u => u.Id)
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Project>()
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .HasPrincipalKey(p => p.Id)
                        .OnDelete(DeleteBehavior.Cascade));
        }
    }
}
