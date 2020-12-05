using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using monakS.BackgroundServices;
using monakS.Hubs;
using monakS.Models;

namespace monakS.Data
{
  public static class AppDbContextExtensions
  {
    // public static EntityEntry<TEntity> UpdateOrAdd<TEntity>(this AppDbContext ctx, TEntity entity) where TEntity : class
    // {
    //   var o = ctx.Find<TEntity>(entity);
    //   return o == null ? ctx.Add(entity) : ctx.Update(entity);
    // }

    // public static void FindTheLatest(this DbSet<CaptureInfo> reviews, int nums)
    // {
    // }
    //
    // public static void FindTheLatest(this IHubContext<MessageHub> reviews, int nums)
    // {
    // }
  }

  public class AppDbContext : DbContext
  {
    public DbSet<Camera> Cameras { get; set; }
    public DbSet<CaptureInfo> CaptureInfos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
      => options.UseSqlite("Data Source=data.db");
  }
}