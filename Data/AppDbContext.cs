using Microsoft.EntityFrameworkCore;
using fraude_pix.Models;

namespace fraude_pix.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TransactionModel> Transactions => Set<TransactionModel>();
        public DbSet<FraudLog> FraudLogs { get; set; }

    }
}
