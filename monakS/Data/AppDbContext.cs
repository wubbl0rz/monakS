using Microsoft.EntityFrameworkCore;
using monakS.BackgroundServices;

namespace monakS.Data
{
  public class AppDbContext : DbContext
  {
    protected override void OnConfiguring(DbContextOptionsBuilder options)
      => options.UseSqlite("Data Source=data.db");
  }
}