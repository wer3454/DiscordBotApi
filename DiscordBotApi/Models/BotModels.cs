namespace BotApi.Models
{
    public class PlayHistory
    {
        public int Id { get; set; }
        public string TrackId { get; set; }
        public string TrackTitle { get; set; }
        public DateTime PlayedAt { get; set; }
        public ulong GuildId { get; set; }
    }
}
