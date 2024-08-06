using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Protocols.Gemini;
using GemBooru.Database;

namespace GemBooru.Endpoints;

public class HomeEndpoints : EndpointGroup
{
    [GeminiEndpoint("/")]
    public string HomePage(RequestContext context, GemBooruDatabaseContext database) => $"""
         # GemBooru

         A simple image booru capsule.

         Currently hosting {database.TotalPostCount()} images uploaded by {database.TotalUserCount()} users!
         
         => /posts View Latest Posts
         => titan://localhost:10061/upload Upload
         """;
}