using System.Globalization;
using System.Text;
using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Core.Responses;
using Bunkum.Core.Storage;
using Bunkum.Listener.Protocol;
using Bunkum.Protocols.Gemini;
using Bunkum.Protocols.Gemini.Responses;
using FFMpegCore;
using GemBooru.Attributes;
using GemBooru.Database;
using GemBooru.Helpers;
using GemBooru.Services;
using Humanizer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace GemBooru.Endpoints;

public class PostEndpoints : EndpointGroup
{
    [GeminiEndpoint("/posts")]
    [GeminiEndpoint("/posts/{page}")]
    [GeminiEndpoint("/posts/{searchType}/{query}")]
    [GeminiEndpoint("/posts/{searchType}/{query}/{page}")]
    [NullStatusCode(NotFound)]
    public string? GetPosts(RequestContext context, GemBooruDatabaseContext database, int? page, string? searchType, string? query)
    {
        const int pageSize = 20;

        var skip = (page ?? 0) * pageSize;
        
        List<DbPost> posts;
        if (searchType != null && query != null)
            switch (searchType)
            {
                case "by_tag":
                    posts = database.GetPostsByTag(skip, pageSize, query).ToList();
                    break;
                case "by_user":
                    posts = database.GetPostsByUser(skip, pageSize, int.Parse(query)).ToList();
                    break;
                default:
                    return null;
            }
        else
            posts = database.GetAllPosts(skip, pageSize).ToList();

        StringBuilder response = new();
        
        response.AppendLine($@"# Showing {posts.Count} posts");
        response.Append('\n');
        foreach (var post in posts)
        {
            response.AppendLine($"## Post by {post.Uploader.Name}");
            response.AppendLine(
                $"### Uploaded {post.UploadDate.Humanize(DateTimeOffset.UtcNow, CultureInfo.InvariantCulture)}");

            response.AppendLine($"=> /post/{post.PostId} View Post");
            response.AppendLine($"=> /user/{post.UploaderId} View User");
            response.AppendLine($"=> {post.GetImageUrl()} View {post.PostType.ToString()}");
        }

        return response.ToString();
    }
    
    [GeminiEndpoint("/post/{postId}")]
    [NullStatusCode(NotFound)]
    public string? GetPost(RequestContext context, GemBooruDatabaseContext database, int postId)
    {
        var post = database.GetPostById(postId);
        if (post == null)
            return null;

        var tags = database.GetTagsForPost(postId);
        
        StringBuilder tagString = new StringBuilder();
        foreach (var tagRelation in tags)
        {
            tagString.AppendFormat("=> /posts/by_tag/{0} {0}\n", tagRelation.Tag);
        }
        
        return $"""
               ## Uploaded {post.UploadDate.Humanize(DateTimeOffset.UtcNow, CultureInfo.InvariantCulture)}
               ## Posted by {post.Uploader.Name}
               => /user/{post.UploaderId} View profile
               
               ## Tags:
               {tagString}
               
               => {post.GetImageUrl()} View {post.PostType.ToString()} 
               
               => /tag/{post.PostId} Add Tag
               """;
    }

    [GeminiEndpoint("/tag/{postId}")]
    [RequiresInput("Please enter the tag")]
    public Response TagPost(RequestContext context, GemBooruDatabaseContext database, int postId, string input)
    {
        input = input.Trim();
        
        var post = database.GetPostById(postId);
        if (post == null) 
            return NotFound;
        
        // If tagging the post fails, return a bad request
        if (!database.TagPost(post, input))
            return new Response("Unable to tag post.", statusCode: BadRequest);
        
        return new Response($$"""
                            Tag {{input}} has been added to post.
                            
                            => /post/{postId} Back to post
                            => /tag/{postId} Tag again
                            """, GeminiContentTypes.Gemtext);
    }
    
    [GeminiEndpoint("/img/{path}")]
    public Response GetImage(RequestContext context, IDataStore dataStore, string path)
    {
        PostType type;
        switch (Path.GetExtension(path))
        {
            case ".png":
                type = PostType.Image;
                break;
            case ".webm":
                type = PostType.Video;
                break;
            case ".ogg":
                type = PostType.Audio;
                break;
            default:
                return new Response($"Unknown extension for file {path}", ContentType.Plaintext, BadRequest);
        }

        return dataStore.ExistsInStore(path)
            ? new Response(dataStore.GetDataFromStore(path), type switch
            {
                PostType.Image => ContentType.Png,
                PostType.Video => ContentType.Webm,
                PostType.Audio => ContentType.Ogg,
            })
            : NotFound;
    }

    [GeminiEndpoint("/upload"), AllowEmptyBody]
    [Authentication(true)]
    public Response UploadPost(
        RequestContext context, 
        DbUser user, 
        GemBooruDatabaseContext database, 
        IDataStore dataStore, 
        AsyncVideoConversionService videoConversionService, 
        GeminiBunkumConfig config,
        byte[] body)
    {
        // Redirect non-titan requests back to the home page
        if (context.Url.Scheme != "titan")
            return new Response("/", GeminiContentTypes.Gemtext, PermanentRedirect);

        // If we do not have a remote certificate, notify the client that one is required
        if (context.RemoteCertificate == null)
            return NetworkAuthenticationRequired;

        const int sizeLimit = 1024 * 1024 * 50;
        
        if (body.Length > sizeLimit)
            return new Response($"Images must be under {sizeLimit.Bytes().ToString()}", ContentType.Plaintext, BadRequest);

        database.SaveChanges();

        var post = database.CreatePost(user.UserId);

        switch (context.RequestHeaders["Content-Type"])
        {
            case ContentType.Apng:
            case ContentType.Png:
            case ContentType.Jpeg:
            case ContentType.Gif:
            case ContentType.Bmp:
            case ContentType.Webp:
            {
                using var img = Image.Load(body);
                
                post.Width = img.Width;
                post.Height = img.Height;

                using var outStream = dataStore.OpenWriteStream(post.PostId + ".png");
                img.SaveAsPng(outStream, new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression
                });
                post.FileSizeInBytes = (int)outStream.Length;
                post.PostType = PostType.Image;

                return new Response($"""
                                     # Post Uploaded!

                                     ## Details

                                     Dimensions: {img.Width:N0}x{img.Height:N0}
                                     Size: {post.FileSizeInBytes.Bytes().ToFullWords()}

                                     => {UrlHelpers.WithSchemeAndPath(config.ExternalUrl, "gemini", $"/post/{post.PostId}")} Go To Post
                                     => /upload Upload again
                                     => {UrlHelpers.WithSchemeAndPath(config.ExternalUrl, "gemini", $"/tag/{post.PostId}")} Tag post
                                     """, GeminiContentTypes.Gemtext);
            }
            case ContentType.Webm:
            case ContentType.Mp4:
            case ContentType.Mpeg:
            case ContentType.Flv:
            {
                var mediaInfo = FFProbe.Analyse(new MemoryStream(body));

                if (mediaInfo.PrimaryVideoStream == null)
                {
                    return new Response($"Video file contains no video streams.", ContentType.Plaintext, BadRequest);
                }

                post.Width = mediaInfo.PrimaryVideoStream.Width;
                post.Height = mediaInfo.PrimaryVideoStream.Height;
                post.PostType = PostType.Video;

                var postId = post.PostId;

                videoConversionService.ConvertVideo(body, postId, postId + ".webm");

                return new Response($"""
                                      # Post Uploaded!
                                      
                                      ## The video is currently being converted, it will be unavailable until that is complete.

                                      ## Details
                                      Dimensions: {mediaInfo.PrimaryVideoStream.Width:N0}x{mediaInfo.PrimaryVideoStream.Height:N0}

                                      => {UrlHelpers.WithSchemeAndPath(config.ExternalUrl, "gemini", $"/post/{post.PostId}")} Go To Post
                                      => /upload Upload again
                                      => {UrlHelpers.WithSchemeAndPath(config.ExternalUrl, "gemini", $"/tag/{post.PostId}")} Tag post
                                      """, GeminiContentTypes.Gemtext);
            }
            default:
                return new Response($"Unknown MIME type {context.RequestHeaders["Content-Type"]}", ContentType.Plaintext, BadRequest);
        }
    }
}