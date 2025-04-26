using Microsoft.EntityFrameworkCore;

namespace PopulateFakeCustomers
{


    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        // If you have entities you want to map, add DbSet properties here.
        // For example: public DbSet<Customer> Customers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Replace with your actual connection string.
                optionsBuilder.UseSqlServer("Your_Connection_String_Here");
            }
        }
    }
}
