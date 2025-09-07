using Microsoft.EntityFrameworkCore;
using URLBox.Models;

namespace URLBox.Context
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
