using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;
namespace Services
{
    public class AppDbContext : DbContext
    {
        public DbSet<GateTransaction> GateTransactions => Set<GateTransaction>();
        public DbSet<GateActions> GateActions => Set<GateActions>();

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=gate.db");
    }
}
