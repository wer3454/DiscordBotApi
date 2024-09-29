using DSharpPlus.Entities;
using DSharpPlus.Commands;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using DSharpPlus.Commands.ContextChecks;
using System.Text;
using BotApi.Models;
using BotApi.Data;

namespace BotApi;

public class MusicCommands
{
    private readonly IAudioService _audioService;
    private readonly BotDbContext _context;

    public MusicCommands(BotDbContext context, IAudioService audioService)
    {
        ArgumentNullException.ThrowIfNull(audioService);
        _context = context;
        _audioService = audioService;
    }

    [Command("play")]
    [Description("Plays music")]
    [DirectMessageUsage(DirectMessageUsage.DenyDMs)]
    public async Task Play(CommandContext context,
        [Parameter("query")]
        [Description("Track to play")]
        string query)
    {
        // This operation could take a while - deferring the interaction lets Discord know we've
        // received it and lets us update it later. Users see a "thinking..." state.
        await context.DeferResponseAsync().ConfigureAwait(false);

        // Attempt to get the player
        var player = await GetPlayerAsync(context, connectToVoiceChannel: true).ConfigureAwait(false);

        // If something went wrong getting the player, don't attempt to play any tracks
        if (player is null)
            return;

        var loadOptions = new TrackLoadOptions
        {
            SearchMode = TrackSearchMode.YouTube
        };
        // Fetch the tracks
        var track = await _audioService.Tracks
            .LoadTrackAsync(query, loadOptions)
            .ConfigureAwait(false);

        // If no results were found
        if (track is null)
        {
            var errorResponse = new DiscordFollowupMessageBuilder()
                .WithContent("😖 No results.")
                .AsEphemeral();

            await context
                .EditResponseAsync(errorResponse)
                .ConfigureAwait(false);

            return;
        }

        // Play the track
        var position = await player
        .PlayAsync(track)
            .ConfigureAwait(false);

        _context.PlayHistory.Add(new PlayHistory
        {
            TrackId = track.Identifier,
            TrackTitle = track.Title,
            PlayedAt = DateTime.UtcNow,
            GuildId = context.Guild.Id
        });

        // If it was added to the queue
        if (position is 0)
        {
            await context
                .FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"🔈 Playing: {track.Uri}"))
                .ConfigureAwait(false);
        }

        // If it was played directly
        else
        {
            await context
                .FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"🔈 Added to queue: {track.Uri}"))
                .ConfigureAwait(false);
        }
    }

    [Command("stop")]
    [Description("Stops the current playback")]
    [DirectMessageUsage(DirectMessageUsage.DenyDMs)]
    public async Task Stop(CommandContext context)
    {
        await context.DeferResponseAsync().ConfigureAwait(false);

        var player = await GetPlayerAsync(context, connectToVoiceChannel: false).ConfigureAwait(false);

        if (player is null)
            return;

        await player.StopAsync().ConfigureAwait(false);

        await context
            .FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("🛑 Playback stopped."))
            .ConfigureAwait(false);
    }

    [Command("skip")]
    [Description("Skips the current track")]
    [DirectMessageUsage(DirectMessageUsage.DenyDMs)]
    public async Task Skip(CommandContext context)
    {
        await context.DeferResponseAsync().ConfigureAwait(false);

        var player = await GetPlayerAsync(context, connectToVoiceChannel: false).ConfigureAwait(false);

        if (player is null)
            return;

        if (player.CurrentTrack is null)
        {
            await context
                .FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Nothing playing!"))
                .ConfigureAwait(false);
            return;
        }

        await player.SkipAsync().ConfigureAwait(false);

        var track = player.CurrentTrack;

        if (track is not null)
        {
            await context
                .FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Skipped. Now playing: {track.Uri}"))
                .ConfigureAwait(false);
        }
        else
        {
            await context
                .FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Skipped. Stopped playing because the queue is now empty."))
                .ConfigureAwait(false);
        }
    }

    [Command("queue")]
    [Description("Displays the current queue")]
    [DirectMessageUsage(DirectMessageUsage.DenyDMs)]
    public async Task ShowQueue(CommandContext context)
    {
        await context.DeferResponseAsync().ConfigureAwait(false);

        var player = await GetPlayerAsync(context, connectToVoiceChannel: false).ConfigureAwait(false);

        if (player is null)
            return;

        var queue = player.Queue;
        var currentTrack = player.CurrentTrack;

        var sb = new StringBuilder();
        sb.AppendLine("🎵 Current Queue:");

        if (currentTrack is not null)
        {
            sb.AppendLine($"Now Playing: {currentTrack.Uri}");
        }

        for (int i = 0; i < queue.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {queue[i].Track.Uri}");
        }

        if (queue.Count == 0 && currentTrack is null)
        {
            sb.AppendLine("The queue is empty.");
        }

        await context
            .FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent(sb.ToString()))
            .ConfigureAwait(false);
    }

    [Command("disconnect")]
    [Description("Disconnects the bot from the voice channel")]
    [DirectMessageUsage(DirectMessageUsage.DenyDMs)]
    public async Task Disconnect(CommandContext context)
    {
        await context.DeferResponseAsync().ConfigureAwait(false);

        var player = await GetPlayerAsync(context, connectToVoiceChannel: false).ConfigureAwait(false);

        if (player is null)
            return;

        await player.DisconnectAsync().ConfigureAwait(false);

        await context
            .FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("👋 Disconnected from voice channel."))
            .ConfigureAwait(false);
    }

    private async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(CommandContext commandContext, bool connectToVoiceChannel = true)
    {
        ArgumentNullException.ThrowIfNull(commandContext);

        var retrieveOptions = new PlayerRetrieveOptions(
            ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

        var playerOptions = new QueuedLavalinkPlayerOptions { HistoryCapacity = 10000 };

        var result = await _audioService.Players
            .RetrieveAsync( commandContext.Guild!.Id, 
                            commandContext.Member?.VoiceState?.Channel?.Id ?? null, 
                            playerFactory: PlayerFactory.Queued, 
                            Options.Create(playerOptions), 
                            retrieveOptions
                          )
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.Status switch
            {
                PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                _ => "Unknown error.",
            };

            var errorResponse = new DiscordFollowupMessageBuilder()
                .WithContent(errorMessage)
                .AsEphemeral();

            await commandContext
                .FollowupAsync(errorResponse)
                .ConfigureAwait(false);

            return null;
        }

        return result.Player;
    }
}