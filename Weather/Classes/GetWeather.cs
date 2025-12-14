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

        public static async Task<DataResponce> Get(float lat, float lon, string city = "")
        {
            var cachedData = await _cacheService.GetCachedWeatherAsync(city, lat, lon);
            if (cachedData != null)
            {
                return cachedData;
            }

            bool canRequest = await _cacheService.CanMakeRequestAsync();
            if (!canRequest)
            {
                throw new Exception("Достигнут дневной лимит запросов. Попробуйте позже.");
            }

            DataResponce dataResponse = null;
            string url = $"{Url}?lat={lat}&lon={lon}".Replace(",", ".");

            using (HttpClient Client = new HttpClient())
            {
                using (HttpRequestMessage Request = new HttpRequestMessage(
                    HttpMethod.Get,
                    url))
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

            if (!canRequest)
            {
                var lastCached = await _cacheService.GetCachedWeatherAsync(city, lat, lon);
                if (lastCached != null)
                {
                    return lastCached;
                }
                throw new Exception("Достигнут дневной лимит запросов и нет кэшированных данных.");
            }

            return dataResponse;
        }
    }
}