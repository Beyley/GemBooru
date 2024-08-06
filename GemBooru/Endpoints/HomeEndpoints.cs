using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Protocols.Gemini;
using GemBooru.Database;
using GemBooru.Helpers;
using Humanizer;

namespace GemBooru.Endpoints;

public class HomeEndpoints : EndpointGroup
{
    [GeminiEndpoint("/")]
    public string HomePage(RequestContext context, GemBooruDatabaseContext database, GeminiBunkumConfig config) => $"""
         # GemBooru

         A simple image booru capsule.

         Currently hosting {"images".ToQuantity(database.TotalPostCount())} uploaded by {"user".ToQuantity(database.TotalUserCount())}!
         
         => /posts View Latest Posts
         => {UrlHelpers.WithSchemeAndPath(config.ExternalUrl, "titan", "upload")} Upload
         """;
}