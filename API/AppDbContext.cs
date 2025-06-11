using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public class AppDbContextAPI : DbContext
    {
        public DbSet<GateTransaction> GateTransactions => Set<GateTransaction>();

        public AppDbContextAPI(DbContextOptions<AppDbContextAPI> options)
      : base(options)
        {
        }
        //protected override void OnConfiguring(DbContextOptionsBuilder options)
        //    => options.UseSqlite("Data Source=gate.db");
    }
}
