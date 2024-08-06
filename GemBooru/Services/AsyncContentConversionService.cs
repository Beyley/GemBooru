using System.Diagnostics;
using Bunkum.Core.Services;
using Bunkum.Core.Storage;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using GemBooru.Database;
using Humanizer;
using NotEnoughLogs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace GemBooru.Services;

public class AsyncContentConversionService : EndpointService
{
    private readonly IDataStore _dataStore;
    private readonly GemBooruDatabaseProvider _databaseProvider;

    internal AsyncContentConversionService(Logger logger, IDataStore dataStore, GemBooruDatabaseProvider databaseProvider) : base(logger)
    {
        _dataStore = dataStore;
        _databaseProvider = databaseProvider;
    }

    public void SerializeImage(Image img, int postId, string dest)
    {
        Task.Factory.StartNew(async () =>
        {
            var timer = Stopwatch.StartNew();

            Logger.LogInfo(GemBooruContext.MediaConversion, $"Starting conversion of image {dest}");
            await using var database = _databaseProvider.GetContext();
            
            try
            {
                await using var outStream = _dataStore.OpenWriteStream(dest);

                await img.SaveAsPngAsync(outStream, new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression
                });
                
                
                Logger.LogInfo(GemBooruContext.MediaConversion, $"Conversion of {dest} completed in {timer.ElapsedMilliseconds.Milliseconds()}!");

                img.Dispose();
                
                var post = database.GetPostById(postId);

                if (post == null)
                    return;
                
                post.Processed = true;
                post.FileSizeInBytes = (int)outStream.Length;

                await database.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                // Delete the post if uploading fails
                database.DeletePost(postId);

                Logger.LogInfo(GemBooruContext.MediaConversion, $"Conversion of {dest} failed in {timer.ElapsedMilliseconds.Milliseconds()}! {ex}");
            }
        });
    }
    
    public void ConvertVideo(byte[] body, int postId, string dest)
    {
        Task.Factory.StartNew(async () =>
        {
            var timer = Stopwatch.StartNew();
            
            Logger.LogInfo(GemBooruContext.MediaConversion, $"Starting conversion of video {dest}");
            await using var database = _databaseProvider.GetContext();
            
            try
            {
                await using var outStream = _dataStore.OpenWriteStream(dest);

                var sourcePath = Path.GetTempFileName();
                await File.WriteAllBytesAsync(sourcePath, body);

                await FFMpegArguments.FromFileInput(sourcePath)
                    .OutputToPipe(new StreamPipeSink(outStream), options => options
                        .WithVideoCodec("vp9")
                        .WithAudioCodec(AudioCodec.LibVorbis)
                        .WithAudioBitrate(AudioQuality.Good)
                        .WithFastStart()
                        .WithoutMetadata()
                        .ForceFormat(VideoType.WebM))
                    .WithLogLevel(FFMpegLogLevel.Verbose)
                    .ProcessAsynchronously();
                
                Logger.LogInfo(GemBooruContext.MediaConversion, $"Conversion of {dest} completed in {timer.ElapsedMilliseconds.Milliseconds()}!");

                var post = database.GetPostById(postId);
                if (post != null)
                {
                    post.Processed = true;
                    post.FileSizeInBytes = (int)outStream.Length;
                    
                    await database.SaveChangesAsync();
                }
            }
            catch(Exception ex)
            {
                // Delete the post if uploading fails
                database.DeletePost(postId);

                Logger.LogInfo(GemBooruContext.MediaConversion, $"Conversion of {dest} failed in {timer.ElapsedMilliseconds.Milliseconds()}! {ex}");
            }
        });
    }
}