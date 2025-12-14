using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Weather.Models
{
    [Table("weather_cache")]
    public class WeatherCache
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("city")]
        [MaxLength(100)]
        public string City { get; set; }

        [Required]
        [Column("latitude")]
        public float Latitude { get; set; }

        [Required]
        [Column("longitude")]
        public float Longitude { get; set; }

        [Required]
        [Column("weather_json", TypeName = "LONGTEXT")]
        public string WeatherJson { get; set; }

        [Required]
        [Column("last_updated")]
        public DateTime LastUpdated { get; set; }

        [Required]
        [Column("valid_until")]
        public DateTime ValidUntil { get; set; }
    }

    [Table("request_logs")]
    public class RequestLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("request_date")]
        public DateTime RequestDate { get; set; }

        [Required]
        [Column("request_count")]
        public int RequestCount { get; set; } = 0;

        [Required]
        [Column("last_request_time")]
        public DateTime LastRequestTime { get; set; }
    }
}