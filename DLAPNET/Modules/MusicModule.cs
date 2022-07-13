using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using FRESHMusicPlayer;
using Shrimpbot.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drawing = SixLabors.ImageSharp;

namespace FRESHMusicBot.Modules
{
    [Name("Music")]
    [Summary("Play music")]
    public class MusicModule : InteractiveBase
    {
        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinChannel(IVoiceChannel channel = null)
        {
            // Get the audio channel
            channel = channel ?? (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null) { await Context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument."); return; }

            // For the next step with transmitting audio, you would want to pass this Audio Client in to a service.
            var audioClient = await channel.ConnectAsync();
            FmpDiscordBackend.FmpDiscordBackend.DiscordAudioClient = audioClient;

            var pathsToPlay = Directory.EnumerateFiles("Songs");

            var player = new Player();
            player.Volume = 0.5f;
            player.Queue.Add(pathsToPlay.ToArray()/*@"D:\Downloads\MEGAREX - SPD GAR(FLAC+BK)\MEGAREX - SPD GAR\04. Don't you want me feat. Such.flac"*/);
            await player.PlayAsync();

            var remotePerson = Context.User.Id;

            async Task SendNowPlaying()
            {
                var z = Drawing.Image.Load(player.Metadata.CoverArt);
                z.Mutate(x => x.Resize(250, 250));
                z.SaveAsJpeg("cover.jpg");
                var fileStream = new FileStream("cover.jpg", FileMode.OpenOrCreate, FileAccess.Read);

                var builder2 = MessagingUtils.GetShrimpbotEmbedBuilder();
                builder2.WithAuthor(Context.Client.GetUser(remotePerson)).WithTitle(":notes: Now Playing")
                .AddField($"{string.Join(",", player.Metadata.Artists)} - {player.Metadata.Title}", $"{player.CurrentTime:mm\\:ss} - {player.TotalTime:mm\\:ss}")
                .AddField("Status", $"**Repeat Mode**: {player.Queue.RepeatMode}\n**Shuffle**: {player.Queue.Shuffle}")
                .WithThumbnailUrl("attachment://cover.jpg")
                .WithFooter("'previous' - Previous; 'next' - Next; 'stop' - Stop; 'np' - View playing track;\n" +
                "'shuffle' - Change shuffle; 'repeat' - Change repeat\n" +
                "'trackinfo' - Detailed track info;\n" +
                "'passremote' - Pass remote; 'queue' - View queue; 'jumpto' - Jump in the queue");
                await Context.Channel.SendFileAsync(fileStream, "cover.jpg", embed: builder2.Build());
                z.Dispose();
                fileStream.Dispose();
            }

            await Context.Channel.SendMessageAsync("You have the remote! To let someone else control playback, say 'passremote'.");
            await SendNowPlaying();
            player.SongChanged += async (sender, args) =>
            {
                await SendNowPlaying();
            };
            while (true)
            {
                var reply = await NextMessageAsync(false, timeout: new TimeSpan(0, 0, 0, 0, -1));

                if (reply.Author.Id != remotePerson) continue;

                switch (reply.Content)
                {
                    case "next":
                        await player.NextAsync();
                        break;
                    case "previous":
                        await player.PreviousAsync();
                        break;
                    case "stop":
                        player.Stop();
                        await channel.DisconnectAsync();
                        return;
                    case "np":
                        await SendNowPlaying();
                        break;
                    case "queue":
                        var tracks = new List<ATL.Track>();
                        foreach (var path in player.Queue.Queue)
                        {
                            var track = new ATL.Track(path);
                            tracks.Add(track);
                        }
                        var sb = new StringBuilder();
                        int i = 1;
                        foreach (var track in tracks)
                        {
                            sb.AppendLine($"{i} | {track.Artist} - {track.Title}");
                            i++;
                        }

                        var builder = MessagingUtils.GetShrimpbotEmbedBuilder();
                        builder.AddField("Queue", sb.ToString());
                        await Context.Channel.SendMessageAsync(embed: builder.Build());
                        break;
                    case "trackinfo":
                        try
                        {
                            using var z = Drawing.Image.Load(player.Metadata.CoverArt);
                            z.Mutate(x => x.Resize(1000, 1000));
                            z.SaveAsJpeg("coverfull.jpg");
                            using var fileStream = new FileStream("coverfull.jpg", FileMode.OpenOrCreate, FileAccess.Read);

                            var builder3 = MessagingUtils.GetShrimpbotEmbedBuilder();
                            builder3.WithTitle("Track Info").WithImageUrl("attachment://coverfull.jpg")
                            .AddField("Disc", $"{player.Metadata.DiscNumber}/{player.Metadata.DiscTotal}", true)
                            .AddField("Track", $"{player.Metadata.DiscNumber}/{player.Metadata.DiscTotal}", true)
                            .AddField("Year", $"{player.Metadata.Year}", true);
                            
                            if (player.Metadata.Genres != null) builder3.AddField("Genre", $"{string.Join(", ", player.Metadata.Genres)}", true);
                            if (player.Metadata.Album != null) builder3.AddField("Album", player.Metadata.Album, true);

                            await Context.Channel.SendFileAsync(fileStream, "coverfull.jpg", embed: builder3.Build());
                        }
                        catch
                        {
                            await Context.Channel.SendMessageAsync("Kyaa! Something went wrong displaying track info. pwease let devs know");
                        }
                        break;
                    default:
                        if (reply.Content.StartsWith("shuffle"))
                        {
                            var words = reply.Content.Split(' ');
                            if (words[1] == "on") player.Queue.Shuffle = true;
                            else if (words[1] == "off") player.Queue.Shuffle = false;
                            else await Context.Channel.SendMessageAsync("Fueh? I don't know what that is. Try saying 'shuffle on' or 'shuffle off'.");
                        }
                        else if (reply.Content.StartsWith("repeat"))
                        {
                            var words = reply.Content.Split(' ');
                            if (words[1] == "one") player.Queue.RepeatMode = RepeatMode.RepeatOne;
                            else if (words[1] == "all") player.Queue.RepeatMode = RepeatMode.RepeatAll;
                            else if (words[1] == "off") player.Queue.RepeatMode = RepeatMode.None;
                            else await Context.Channel.SendMessageAsync("Fueh? I don't know what that is. Try saying 'repeat one', 'repeat all', or 'repeat off'.");
                        }
                        else if (reply.Content.StartsWith("passremote"))
                        {
                            var words = reply.Content.Split(' ');
                            //var userToSendTo = MentionUtils.ParseUser(words[1]);
                            //var userX = Context.Client.GetUser(ulong.Parse(words[1]));
                            remotePerson = ulong.Parse(words[1]);
                        }
                        break;
                }
            }
        }
    }
}
