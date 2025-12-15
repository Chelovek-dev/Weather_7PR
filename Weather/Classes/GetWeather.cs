using Newtonsoft.Json;
using System.Net.Http;
using Weather.Models;
using Weather.Services;

namespace Weather.Classes
{
    public class GetWeather
    {
        public static string Url = "https://api.weather.yandex.ru/v2/forecast";
        public static string Key = "demo_yandex_weather_api_key_ca6d09349ba0";
        private static CacheService _cacheService = new CacheService();

        public static async Task<DataResponce> Get(float lat, float lon, string city = "", bool forceApi = false)
        {
            if (string.IsNullOrEmpty(city))
            {
                city = "Unknown";
            }

            bool shouldUseApi = forceApi || await _cacheService.ShouldUpdateFromApiAsync(city, lat, lon);

            if (!shouldUseApi)
            {
                var cachedData = await _cacheService.GetCachedWeatherAsync(city, lat, lon);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            bool canRequest = await _cacheService.CanMakeRequestAsync();
            if (!canRequest)
            {
                var cachedData = await _cacheService.GetCachedWeatherAsync(city, lat, lon);
                if (cachedData != null)
                {
                    return cachedData;
                }

                var remaining = await _cacheService.GetRemainingRequestsAsync();
                var nextTime = await _cacheService.GetNextRequestTimeAsync();

                if (nextTime.HasValue)
                {
                    var timeLeft = nextTime.Value - System.DateTime.Now;
                    throw new System.Exception($"Достигнут лимит запросов. Следующий запрос через {timeLeft.Minutes} минут {timeLeft.Seconds} секунд. Осталось запросов сегодня: {remaining}");
                }
                else
                {
                    throw new System.Exception($"Достигнут дневной лимит запросов. Осталось запросов: {remaining}");
                }
            }

            DataResponce dataResponse = null;
            string url = $"{Url}?lat={lat}&lon={lon}".Replace(",", ".");

            using (HttpClient Client = new HttpClient())
            {
                using (HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    Request.Headers.Add("X-Yandex-Weather-Key", Key);

                    using (var Response = await Client.SendAsync(Request))
                    {
                        string ContentResponse = await Response.Content.ReadAsStringAsync();
                        dataResponse = JsonConvert.DeserializeObject<DataResponce>(ContentResponse);
                    }
                }
            }

            if (dataResponse != null && !string.IsNullOrEmpty(city))
            {
                await _cacheService.SaveToCacheAsync(city, lat, lon, dataResponse);
            }

            await _cacheService.RegisterRequestAsync();

            return dataResponse;
        }
    }
}