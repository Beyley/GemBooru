using System.Text;
using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Protocols.Gemini;
using GemBooru.Database;
using Humanizer;

namespace GemBooru.Endpoints;

public class UserEndpoints : EndpointGroup
{
    [GeminiEndpoint("/user/{userId}")]
    [NullStatusCode(NotFound)]
    public string? GetUserById(RequestContext context, DbUser? loggedInUser, GemBooruDatabaseContext database, int userId)
    {
        var user = database.GetUserById(userId);
        if (user == null)
            return null;

        var response = new StringBuilder();

        response.AppendLine($"""
                         # {user.Name}

                         ## Made {"post".ToQuantity(database.GetAllPostsByUser(user.UserId).Count())}
                         
                         => /posts/by_user/{userId} Posts
                         """);

        if (loggedInUser == user)
            response.AppendLine($"""
                                 => /user_settings User Settings
                                 """
            );
        
        return response.ToString();
    }
}