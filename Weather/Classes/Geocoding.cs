using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Weather.Classes
{
    public class Geocoding
    {
        public static string Url = "https://geocode-maps.yandex.ru/v1";
        public static string ApiKey = "d4873627-dab6-4acf-baee-4e6c0a61df9c";

        public class GeoResponse
        {
            public GeoResponseInner response { get; set; }
        }
        public class GeoResponseInner
        {
            public GeoObjectCollection GeoObjectCollection { get; set; }
        }

        public class GeoObjectCollection
        {
            public FeatureMember[] featureMember { get; set; }
        }

        public class FeatureMember
        {
            public GeoObject GeoObject { get; set; }
        }

        public class GeoObject
        {
            public Point Point { get; set; }
        }

        public class Point
        {
            public string pos { get; set; }
        }

        public static async Task<(float lat, float lon)> GetCoordinates(string address)
        {
            string url = $"{Url}?apikey={ApiKey}&geocode={address}&format=json";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                string content = await response.Content.ReadAsStringAsync();

                var geoResponse = JsonConvert.DeserializeObject<GeoResponse>(content);

                if (geoResponse?.response?.GeoObjectCollection?.featureMember?.Length > 0)
                {
                    string pos = geoResponse.response.GeoObjectCollection.featureMember[0].GeoObject.Point.pos;
                    string[] coordinates = pos.Split(' ');

                    if (coordinates.Length == 2)
                    {
                        float lon = float.Parse(coordinates[0], System.Globalization.CultureInfo.InvariantCulture);
                        float lat = float.Parse(coordinates[1], System.Globalization.CultureInfo.InvariantCulture);
                        return (lat, lon);
                    }
                }
            }

            // по умолчанию Пермь
            return (58.009671f, 56.226184f);
        }
    }
}