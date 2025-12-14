using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using Weather.Models;

namespace Weather.Data
{
    public class WeatherDbContext : DbContext
    {
        public DbSet<WeatherCache> WeatherCaches { get; set; }
        public DbSet<RequestLog> RequestLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = "server=localhost;database=weather_app;user=root;pwd=;";
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RequestLog>()
                .HasIndex(r => r.RequestDate)
                .IsUnique();
        }
    }
}