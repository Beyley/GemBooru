using System.Text;
using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Core.Responses;
using Bunkum.Protocols.Gemini;
using GemBooru.Attributes;
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
                         
                         ## Biography
                         {user.Bio}

                         ## Made {"post".ToQuantity(database.GetPostsByUser(user.UserId).Count())}
                         
                         => /posts/by_user/{userId} Posts
                         """);

        if (loggedInUser == user)
            response.AppendLine("=> /user_settings User Settings");
        
        return response.ToString();
    }

    [GeminiEndpoint("/user_settings")]
    [Authentication(true)]
    public string UserSettings(RequestContext context, DbUser user)
    {
        var response = new StringBuilder();

        response.Append(
            $"""
            # Welcome {user.Name}!
            
            On this page you can change various settings.
            
            => /user_settings/name Change Name
            => /user_settings/bio Change Biography
            """
            );
        
        return response.ToString();
    }

    [GeminiEndpoint("/user_settings/name")]
    [Authentication(true)]
    [RequiresInput("Please enter the new name")]
    public Response ChangeUserName(RequestContext context, DbUser user, string input, GemBooruDatabaseContext database)
    {
        // Change the name
        user.Name = input.Trim();
        
        database.SaveChanges();

        // Redirect back to the user settings page after its updated
        return new Response("/user_settings", statusCode: Redirect);
    }

    [GeminiEndpoint("/user_settings/bio")]
    [Authentication(true)]
    [RequiresInput("Please enter your new bio")]
    public Response ChangeBio(RequestContext context, DbUser user, string input, GemBooruDatabaseContext database)
    {
        user.Bio = input.Trim();

        database.SaveChanges();

        return new Response("/user_settings", statusCode: Redirect);
    }
    
}