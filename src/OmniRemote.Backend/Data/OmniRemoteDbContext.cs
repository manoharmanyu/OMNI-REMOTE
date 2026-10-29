using Microsoft.EntityFrameworkCore;

namespace OmniRemote.Backend.Data;

public sealed class OmniRemoteDbContext : DbContext
{
    public OmniRemoteDbContext(DbContextOptions<OmniRemoteDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<StoredDeviceEntity> Devices => Set<StoredDeviceEntity>();
    public DbSet<StoredActivityEntity> Activities => Set<StoredActivityEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserEntity>(b =>
        {
            b.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<StoredDeviceEntity>(b =>
        {
            b.HasIndex(d => d.UserId);
            b.HasIndex(d => new { d.UserId, d.Id });
        });

        modelBuilder.Entity<StoredActivityEntity>(b =>
        {
            b.HasIndex(a => a.UserId);
            b.HasIndex(a => new { a.UserId, a.DeviceId });
            b.HasIndex(a => a.Timestamp);
        });
    }
}
