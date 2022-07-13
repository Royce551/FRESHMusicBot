using Discord.Audio;
using FRESHMusicPlayer.Backends;
using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FmpDiscordBackend
{
    [Export(typeof(IAudioBackend))]
    public class FmpDiscordBackend : IAudioBackend
    {
        public static IAudioClient DiscordAudioClient { get; set; }

        public TimeSpan CurrentTime
        {
            get => stop.Elapsed;
            set => throw new NotSupportedException();
        }
        public TimeSpan TotalTime => TimeSpan.FromSeconds(fil.Length);
        public float Volume { get; set; }

        public event EventHandler<EventArgs> OnPlaybackStopped;

        private string filePath;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Task task;
        private FileMetadataProvider fil;
        private Stopwatch stop = new Stopwatch();

        public FmpDiscordBackend()
        {
            stop.Start();
        }

        public void Dispose()
        {
            cts.Cancel();
        }

        public async Task<IMetadataProvider> GetMetadataAsync(string file)
        {
            await Task.Run(() =>
            {
                fil = new FileMetadataProvider(file);
            });
            return fil;
        }

        public Task<BackendLoadResult> LoadSongAsync(string file)
        {
            filePath = file;
            return Task.FromResult(BackendLoadResult.OK);
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public async void Play()
        {
            task = Task.Run(async () =>
                {
                    using (var ffmpeg = CreateStream(filePath))
                    using (var output = ffmpeg.StandardOutput.BaseStream)
                    using (var discord = DiscordAudioClient.CreatePCMStream(AudioApplication.Music))
                    {
                        try { await output.CopyToAsync(discord, 81920, cts.Token); }
                        finally { await discord.FlushAsync(cts.Token); }
                    }
                }, cts.Token);
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            OnPlaybackStopped?.Invoke(null, EventArgs.Empty);
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
    }
}
