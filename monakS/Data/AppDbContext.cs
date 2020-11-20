using Microsoft.EntityFrameworkCore;
using monakS.BackgroundServices;

namespace monakS.Data
{
  public class AppDbContext : DbContext
  {
    public DbSet<CaptureInfo> CaptureInfos { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
      => options.UseSqlite("Data Source=data.db");
  }
}