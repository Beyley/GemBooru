using Bunkum.Core.Configuration;

namespace GemBooru;

public class GemBooruConfig : Config
{
    public override int CurrentConfigVersion => 1;
    public override int Version { get; set; }

    public string PostgresSqlConnectionString { get; set; } = "Host=localhost;Username=username;Password=password;Database=gembooru";
    
    protected override void Migrate(int oldVer, dynamic oldConfig)
    {
    }
}