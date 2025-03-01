using Microsoft.EntityFrameworkCore;
using MyUrls.Models;

namespace MyUrls.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UrlModel> Urls { get; set; }
    }
}
