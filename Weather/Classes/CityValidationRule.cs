using System.Globalization;
using System.Windows.Controls;

namespace Weather
{
    public class CityValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string city = value as string;

            if (string.IsNullOrWhiteSpace(city))
            {
                return new ValidationResult(false, "Введите название города");
            }

            if (city.Length < 2)
            {
                return new ValidationResult(false, "Название города слишком короткое");
            }

            return ValidationResult.ValidResult;
        }
    }
}