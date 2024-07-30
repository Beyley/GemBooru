using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Core.Responses;
using Bunkum.Core.Storage;
using Bunkum.Listener.Protocol;
using Bunkum.Protocols.Gemini;
using Bunkum.Protocols.Gemini.Responses;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using GemBooru.Database;
using GemBooru.Services;
using Humanizer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace GemBooru.Endpoints;

public class PostEndpoints : EndpointGroup
{
    [GeminiEndpoint("/posts")]
    [GeminiEndpoint("/posts/{page}")]
    public string GetPosts(RequestContext context, GemBooruDatabaseContext database, int page)
    {
        const int pageSize = 20;

        var posts = database.GetPosts(page * pageSize, pageSize).ToList();

        StringBuilder response = new();
        
        response.AppendLine($@"# Showing {posts.Count} posts");
        response.Append('\n');
        foreach (var post in posts)
        {
            response.AppendLine($"## Post by {post.Uploader.Name}");
            response.AppendLine(
                $"### Uploaded {post.UploadDate.Humanize(true, DateTime.UtcNow, CultureInfo.InvariantCulture)}");

            response.AppendLine($"=> /post/{post.PostId} View Post");
            response.AppendLine($"=> /user/{post.UploaderId} View User");
            switch (post.PostType)
            {
                case PostType.Image:
                    response.AppendLine($"=> /img/{post.PostId}.png View Image");
                    break;
                case PostType.Video:
                    response.AppendLine($"=> /img/{post.PostId}.webm View Video");
                    break;
                case PostType.Audio:
                    response.AppendLine($"=> /img/{post.PostId}.ogg View Audio");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return response.ToString();
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
    public Response UploadPost(RequestContext context, GemBooruDatabaseContext database, IDataStore dataStore, AsyncVideoConversionService videoConversionService, byte[] body)
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

        var certHash = context.RemoteCertificate.GetCertHashString(HashAlgorithmName.SHA256);

        var uploader = database.CreateOrGetUser(certHash, "Unnamed User");

        uploader.Name = "Unnamed User " + uploader.UserId;

        database.SaveChanges();

        var post = database.CreatePost(uploader.UserId);

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

                                     => /post/{post.PostId} Go To Post
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

                                     => /post/{postId} Go To Post
                                     """, GeminiContentTypes.Gemtext);
            }
            default:
                return new Response($"Unknown MIME type {context.RequestHeaders["Content-Type"]}", ContentType.Plaintext, BadRequest);
        }
    }
}