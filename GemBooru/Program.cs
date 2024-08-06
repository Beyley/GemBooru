//

using Bunkum.Core;
using Bunkum.Core.Configuration;
using Bunkum.Core.Storage;
using Bunkum.Protocols.Gemini;
using GemBooru;
using GemBooru.Authentication;
using GemBooru.Database;
using GemBooru.Services;
using NotEnoughLogs;
using NotEnoughLogs.Behaviour;

BunkumServer server = new BunkumGeminiServer(null, new LoggerConfiguration
{
    Behaviour = new QueueLoggingBehaviour(),
#if DEBUG
    MaxLevel = LogLevel.Trace,
#else
    MaxLevel = LogLevel.Info,
#endif
});

server.Initialize = s =>
{
    var config = Config.LoadFromJsonFile<GemBooruConfig>("gembooru.json", s.Logger);

    IDataStore dataStore = new FileSystemDataStore();
    
    s.DiscoverEndpointsFromAssembly(typeof(Program).Assembly);
    s.AddStorageService(dataStore);
    s.AddConfig(config);

    GemBooruDatabaseProvider databaseProvider = new(config);
    s.UseDatabaseProvider(databaseProvider);
    s.AddService(new AsyncContentConversionService(s.Logger, dataStore, databaseProvider));
    s.AddService<RequiresInputService>();
    s.AddAuthenticationService(new CertificateAuthenticationProvider(), failureStatusCode: Unauthorized);
    
};

server.Start();
await Task.Delay(-1);