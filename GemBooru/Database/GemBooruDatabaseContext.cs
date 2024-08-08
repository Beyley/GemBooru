using Bunkum.Core.Database;
using Microsoft.EntityFrameworkCore;

namespace GemBooru.Database;

public partial class GemBooruDatabaseContext : DbContext, IDatabaseContext
{
    private readonly GemBooruConfig _config;

    public GemBooruDatabaseContext()
    {
        this._config = new GemBooruConfig();
    }

    public GemBooruDatabaseContext(GemBooruConfig config)
    {
        _config = config;
    }

    private DbSet<DbPost> Posts { get; set; }
    private DbSet<DbTagRelation> TagRelations { get; set; }
    private DbSet<DbUser> Users { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        => optionsBuilder.UseNpgsql(_config.PostgresSqlConnectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbTagRelation>()
            .HasIndex(p => new {p.Tag , p.PostId}).IsUnique();
        
        modelBuilder.Entity<DbPost>()  
            .Property(b => b.Processed)  
            .HasDefaultValue(true);
    }

    public override void Dispose()
    {
        SaveChanges();
        base.Dispose();
    }
}