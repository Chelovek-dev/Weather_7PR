using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Weather.Data;
using Weather.Models;

namespace Weather.Services
{
    public class CacheService
    {
        private readonly WeatherDbContext _dbContext;
        private const int DAILY_REQUEST_LIMIT = 50;
        private const int CACHE_VALID_HOURS = 3;

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        public CacheService()
        {
            _dbContext = new WeatherDbContext();
        }

        public async Task<bool> CanMakeRequestAsync()
        {
            var today = DateTime.Today;

            using (var db = new WeatherDbContext())
            {
                var log = await db.RequestLogs
                    .FirstOrDefaultAsync(r => r.RequestDate == today);

                if (log == null)
                {
                    return true;
                }

                if (log.RequestCount >= DAILY_REQUEST_LIMIT)
                {
                    return false;
                }

                var timeSinceLastRequest = DateTime.Now - log.LastRequestTime;
                if (timeSinceLastRequest.TotalMinutes < 1.0)
                {
                    return false;
                }

                return true;
            }
        }

        public async Task RegisterRequestAsync()
        {
            var today = DateTime.Today;
            var now = DateTime.Now;

            using (var db = new WeatherDbContext())
            {
                var log = await db.RequestLogs
                    .FirstOrDefaultAsync(r => r.RequestDate == today);

                if (log == null)
                {
                    log = new RequestLog
                    {
                        RequestDate = today,
                        RequestCount = 1,
                        LastRequestTime = now
                    };
                    await db.RequestLogs.AddAsync(log);
                }
                else
                {
                    log.RequestCount++;
                    log.LastRequestTime = now;
                    db.RequestLogs.Update(log);
                }

                await db.SaveChangesAsync();
            }
        }

        public async Task<DataResponce> GetCachedWeatherAsync(string city, float lat, float lon)
        {
            using (var db = new WeatherDbContext())
            {
                var cache = await db.WeatherCaches
                    .Where(c => c.City == city &&
                           Math.Abs(c.Latitude - lat) < 0.001 &&
                           Math.Abs(c.Longitude - lon) < 0.001 &&
                           c.ValidUntil > DateTime.Now)
                    .OrderByDescending(c => c.LastUpdated)
                    .FirstOrDefaultAsync();

                if (cache != null)
                {
                    return JsonConvert.DeserializeObject<DataResponce>(cache.WeatherJson);
                }

                return null;
            }
        }

        public async Task SaveToCacheAsync(string city, float lat, float lon, DataResponce weatherData)
        {
            using (var db = new WeatherDbContext())
            {
                var existing = await db.WeatherCaches
                    .FirstOrDefaultAsync(c => c.City == city &&
                           Math.Abs(c.Latitude - lat) < 0.001 &&
                           Math.Abs(c.Longitude - lon) < 0.001);

                var cacheEntry = new WeatherCache
                {
                    City = city,
                    Latitude = lat,
                    Longitude = lon,
                    WeatherJson = JsonConvert.SerializeObject(weatherData),
                    LastUpdated = DateTime.Now,
                    ValidUntil = DateTime.Now.AddHours(CACHE_VALID_HOURS)
                };

                if (existing != null)
                {
                    cacheEntry.Id = existing.Id;
                    db.WeatherCaches.Update(cacheEntry);
                }
                else
                {
                    await db.WeatherCaches.AddAsync(cacheEntry);
                }

                await db.SaveChangesAsync();
            }
        }

        public async Task CleanupOldCacheAsync()
        {
            using (var db = new WeatherDbContext())
            {
                var expired = await db.WeatherCaches
                    .Where(c => c.ValidUntil <= DateTime.Now)
                    .ToListAsync();

                if (expired.Any())
                {
                    db.WeatherCaches.RemoveRange(expired);
                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task ResetOldLogsAsync()
        {
            using (var db = new WeatherDbContext())
            {
                var oldLogs = await db.RequestLogs
                    .Where(r => r.RequestDate < DateTime.Today.AddDays(-7))
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    db.RequestLogs.RemoveRange(oldLogs);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}