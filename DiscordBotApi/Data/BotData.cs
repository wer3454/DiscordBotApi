using BotApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BotApi.Data
{
    public class BotDbContext : DbContext
    {
        public BotDbContext(DbContextOptions<BotDbContext> options) : base(options) { }

        public DbSet<PlayHistory> PlayHistory { get; set; }
    }
}
