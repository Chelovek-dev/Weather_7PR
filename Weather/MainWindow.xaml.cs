using System;
using System.Windows;
using Weather.Classes;
using Weather.Models;

namespace Weather
{
    public partial class MainWindow : Window
    {
        DataResponce responce;
        private float currentLat = 58.009671f;
        private float currentLon = 56.226184f;
        private string currentCity = "Пермь, Россия";

        public MainWindow()
        {
            InitializeComponent();
            LocationText.Text = currentCity;
            Iint();
        }

        public async void Iint()
        {
            parent.Children.Clear();
            Days.Items.Clear();

            try
            {
                responce = await GetWeather.Get(currentLat, currentLon);

                foreach (Forecast forecast in responce.forecasts)
                {
                    Days.Items.Add(forecast.date.ToString("dd.MM.yyyy"));
                }

                if (Days.Items.Count > 0)
                {
                    Days.SelectedIndex = 0;
                }
                Create(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки погоды: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Create(int idForecast)
        {
            parent.Children.Clear();
            foreach (Hour hour in responce.forecasts[idForecast].hours)
            {
                parent.Children.Add(new Elements.Item(hour));
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string cityName = CityTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(cityName))
            {
                MessageBox.Show("Введите название города", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SearchButton.IsEnabled = false;
                SearchButton.Content = "Поиск...";

                var coordinates = await Geocoding.GetCoordinates(cityName);
                currentLat = coordinates.lat;
                currentLon = coordinates.lon;
                currentCity = cityName;

                LocationText.Text = cityName;

                Iint();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска города: {ex.Message}\nПроверьте название и попробуйте снова.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SearchButton.IsEnabled = true;
                SearchButton.Content = "Найти";
            }
        }

        private void SelectDay(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Days.SelectedIndex >= 0)
            {
                Create(Days.SelectedIndex);
            }
        }

        private void UpdateWeather(object sender, RoutedEventArgs e)
        {
            Iint();
        }
    }
}