using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BotApi.Data;
using BotApi.Models;
using Lavalink4NET;
using DSharpPlus;
using Lavalink4NET.Rest.Entities.Tracks;

namespace BotApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BotApiController : ControllerBase
{
    private readonly BotDbContext _context;
    private readonly IAudioService _audioService;
    private readonly DiscordClient _discordClient;

    public BotApiController(BotDbContext context, IAudioService audioService, DiscordClient discordClient)
    {
        _context = context;
        _audioService = audioService;
        _discordClient = discordClient;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalPlays = await _context.PlayHistory.CountAsync();
        var uniqueTracks = await _context.PlayHistory.Select(p => p.TrackId).Distinct().CountAsync();
        var connectedServers = _discordClient.Guilds.Count;

        return Ok(new
        {
            TotalPlays = totalPlays,
            UniqueTracks = uniqueTracks,
            ConnectedServers = connectedServers
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetPlayHistory(int page = 1, int pageSize = 10)
    {
        var history = await _context.PlayHistory
            .OrderByDescending(p => p.PlayedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(history);
    }

    [HttpPost("play")]
    public async Task<IActionResult> PlayTrack([FromBody] PlayRequest request)
    {
        var player = await _audioService.Players.GetPlayerAsync(request.GuildId);
        if (player == null)
        {
            return NotFound("No player found for this guild");
        }

        var loadOptions = new TrackLoadOptions
        {
            SearchMode = TrackSearchMode.YouTube
        };

        var track = await _audioService.Tracks.LoadTrackAsync(request.Query, loadOptions);
        if (track == null)
        {
            return NotFound("No track found");
        }

        await player.PlayAsync(track);

        _context.PlayHistory.Add(new PlayHistory
        {
            TrackId = track.Identifier,
            TrackTitle = track.Title,
            PlayedAt = DateTime.UtcNow,
            GuildId = request.GuildId
        });
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Track added to queue", TrackTitle = track.Title });
    }
}

public class PlayRequest
{
    public ulong GuildId { get; set; }
    public string Query { get; set; }
}