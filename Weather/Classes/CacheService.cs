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
        private const int DAILY_REQUEST_LIMIT = 50;
        private const int CACHE_VALID_HOURS = 3;
        private const int REQUEST_INTERVAL_MINUTES = 30;

        public void Dispose()
        {
        }

        public CacheService()
        {
        }

        public async Task<bool> CanMakeRequestAsync()
        {
            var today = DateTime.Today;
            var now = DateTime.Now;

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

                var timeSinceLastRequest = now - log.LastRequestTime;
                if (timeSinceLastRequest.TotalMinutes < REQUEST_INTERVAL_MINUTES)
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
                    db.Entry(log).State = EntityState.Modified;
                }

                await db.SaveChangesAsync();
            }
        }

        public async Task<DataResponce> GetCachedWeatherAsync(string city, float lat, float lon)
        {
            using (var db = new WeatherDbContext())
            {
                var cache = await db.WeatherCaches
                    .AsNoTracking()
                    .Where(c => c.City.ToLower() == city.ToLower() &&
                           Math.Abs(c.Latitude - lat) < 0.001 &&
                           Math.Abs(c.Longitude - lon) < 0.001)
                    .OrderByDescending(c => c.LastUpdated)
                    .FirstOrDefaultAsync();

                if (cache != null)
                {
                    return JsonConvert.DeserializeObject<DataResponce>(cache.WeatherJson);
                }

                return null;
            }
        }

        public async Task<bool> ShouldUpdateFromApiAsync(string city, float lat, float lon)
        {
            var canMakeRequest = await CanMakeRequestAsync();
            if (!canMakeRequest)
            {
                return false;
            }

            using (var db = new WeatherDbContext())
            {
                var cache = await db.WeatherCaches
                    .AsNoTracking()
                    .Where(c => c.City.ToLower() == city.ToLower() &&
                           Math.Abs(c.Latitude - lat) < 0.001 &&
                           Math.Abs(c.Longitude - lon) < 0.001)
                    .FirstOrDefaultAsync();

                if (cache == null)
                {
                    return true;
                }

                var timeSinceLastUpdate = DateTime.Now - cache.LastUpdated;
                return timeSinceLastUpdate.TotalHours >= 1;
            }
        }

        public async Task SaveToCacheAsync(string city, float lat, float lon, DataResponce weatherData)
        {
            using (var db = new WeatherDbContext())
            {
                var existing = await db.WeatherCaches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.City.ToLower() == city.ToLower() &&
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

        public async Task<int> GetRemainingRequestsAsync()
        {
            var today = DateTime.Today;

            using (var db = new WeatherDbContext())
            {
                var log = await db.RequestLogs
                    .FirstOrDefaultAsync(r => r.RequestDate == today);

                if (log == null)
                {
                    return DAILY_REQUEST_LIMIT;
                }

                return Math.Max(0, DAILY_REQUEST_LIMIT - log.RequestCount);
            }
        }

        public async Task<DateTime?> GetNextRequestTimeAsync()
        {
            var today = DateTime.Today;

            using (var db = new WeatherDbContext())
            {
                var log = await db.RequestLogs
                    .FirstOrDefaultAsync(r => r.RequestDate == today);

                if (log == null)
                {
                    return null;
                }

                return log.LastRequestTime.AddMinutes(REQUEST_INTERVAL_MINUTES);
            }
        }
    }
}