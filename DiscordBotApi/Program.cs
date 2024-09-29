using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Extensions;
using BotApi;
using Lavalink4NET.Extensions;
using Microsoft.EntityFrameworkCore;
using BotApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Your host service
builder.Services.AddHostedService<ApplicationHost>();

string? token = Environment.GetEnvironmentVariable("DiscordToken");
if (string.IsNullOrEmpty(token))
{
    throw new Exception("No token provided");
}

// DSharpPlus
builder.Services.AddDiscordClient(token, DiscordIntents.AllUnprivileged);
builder.Services.AddCommandsExtension(extension => extension.AddCommands(typeof(MusicCommands).Assembly));

// Lavalink4NET
string lavalinkaddress = "lavalink";
builder.Services.AddLavalink();
builder.Services.ConfigureLavalink(config =>
{
    config.ReadyTimeout = TimeSpan.FromSeconds(60);
    config.WebSocketUri = new Uri($"ws://{lavalinkaddress}:2333/v4/websocket");
    config.BaseAddress = new Uri($"http://{lavalinkaddress}:2333");
});

// Database
builder.Services.AddDbContext<BotDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


// Web API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Logging
builder.Services.AddLogging(s => s.AddConsole().SetMinimumLevel(LogLevel.Information));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


await app.RunAsync();

file sealed class ApplicationHost : BackgroundService
{
    private readonly DiscordClient _discordClient;

    public ApplicationHost(DiscordClient discordClient)
    {
        ArgumentNullException.ThrowIfNull(discordClient);
        _discordClient = discordClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Connect to discord gateway and initialize node connection
        await _discordClient
            .ConnectAsync()
            .ConfigureAwait(false);

        await Task
            .Delay(-1, stoppingToken)
            .ConfigureAwait(false);
    }
}