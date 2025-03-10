using BATTARI_api.Models;
using BATTARI_api.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace BATTARI_api.Repository.Data;

public class UserContext(IConfiguration configuration) : DbContext
{
    public DbSet<UserModel> Users { get; set; }
    public DbSet<FriendModel> Friends { get; set; }
    public DbSet<RefreshTokenModel> RefreshTokens { get; set; }
    public DbSet<CallModel> Calls { get; set; }
    public DbSet<SummarizationResultModel> SummarizationResults { get; set; }

    protected override void
    OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
            optionsBuilder.UseMySql(configuration["ConnectionString"], new MySqlServerVersion(new Version(8, 0, 25)));
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserModel>().ToTable("Users");
    }
}
