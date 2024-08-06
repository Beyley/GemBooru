using Bunkum.EntityFrameworkDatabase;
using Microsoft.EntityFrameworkCore;

namespace GemBooru.Database;

public class GemBooruDatabaseProvider(GemBooruConfig config) : EntityFrameworkDatabaseProvider<GemBooruDatabaseContext>
{
    protected override EntityFrameworkInitializationStyle InitializationStyle =>
        EntityFrameworkInitializationStyle.EnsureCreated;

    public override GemBooruDatabaseContext GetContext() => new(config);
}       