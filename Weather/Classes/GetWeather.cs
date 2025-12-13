using Newtonsoft.Json;
using System.Net.Http;
using Weather.Models;

namespace Weather.Classes
{
    public class GetWeather
    {
        public static string Url = "https://api.weather.yandex.ru/v2/forecast";
        public static string Key = "demo_yandex_weather_api_key_ca6d09349ba0";


        public static async Task<DataResponce> Get(float lat, float lon)
        {
            DataResponce DataResponse = null;
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
                        DataResponse = JsonConvert.DeserializeObject<DataResponce>(ContentResponse);
                    }    
                    return DataResponse;

                }
            }
        }
    }
}