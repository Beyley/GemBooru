using Bunkum.Core.Services;
using Bunkum.Core.Storage;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using GemBooru.Database;
using NotEnoughLogs;

namespace GemBooru.Services;

public class AsyncVideoConversionService : EndpointService
{
    private readonly IDataStore _dataStore;
    private readonly GemBooruDatabaseProvider _databaseProvider;

    internal AsyncVideoConversionService(Logger logger, IDataStore dataStore, GemBooruDatabaseProvider databaseProvider) : base(logger)
    {
        _dataStore = dataStore;
        _databaseProvider = databaseProvider;
    }

    public void ConvertVideo(byte[] body, int postId, string dest)
    {
        Task.Factory.StartNew(async () =>
        {
            Logger.LogInfo(GemBooruContext.VideoConversion, $"Starting conversion of video {dest}");
            await using var database = _databaseProvider.GetContext();
            
            try
            {
                await using var outStream = _dataStore.OpenWriteStream(dest);

                var sourcePath = Path.GetTempFileName();
                await File.WriteAllBytesAsync(sourcePath, body);

                await FFMpegArguments.FromFileInput(sourcePath)
                    .OutputToPipe(new StreamPipeSink(outStream), options => options
                        .WithVideoCodec(VideoCodec.LibVpx)
                        .WithAudioCodec(AudioCodec.LibVorbis)
                        .WithAudioBitrate(AudioQuality.Good)
                        .WithFastStart()
                        .WithoutMetadata()
                        .ForceFormat(VideoType.WebM))
                    .WithLogLevel(FFMpegLogLevel.Verbose)
                    .ProcessAsynchronously();
                
                Logger.LogInfo(GemBooruContext.VideoConversion, $"Conversion of {dest} completed!");
            }
            catch(Exception ex)
            {
                // Delete the post if uploading fails
                database.DeletePost(postId);

                Logger.LogInfo(GemBooruContext.VideoConversion, $"Conversion of {dest} failed! {ex}");
            }
        });
    }
}