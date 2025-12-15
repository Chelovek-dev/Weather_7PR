using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Weather.Classes;
using Weather.Models;
using Weather.Services;

namespace Weather
{
    public partial class MainWindow : Window
    {
        DataResponce responce;
        private float currentLat = 58.009671f;
        private float currentLon = 56.226184f;
        private string currentCity = "Пермь";
        private readonly CacheService _cacheService;
        private readonly DispatcherTimer _cleanupTimer;

        public MainWindow()
        {
            InitializeComponent();
            _cacheService = new CacheService();
            LocationText.Text = currentCity;

            _cleanupTimer = new DispatcherTimer();
            _cleanupTimer.Interval = TimeSpan.FromHours(6);
            _cleanupTimer.Tick += CleanupTimer_Tick;
            _cleanupTimer.Start();

            InitializeApplication();
        }

        private async void InitializeApplication()
        {
            try
            {
                await _cacheService.CleanupOldCacheAsync();
                await LoadWeatherData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CleanupTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                await _cacheService.CleanupOldCacheAsync();
            }
            catch
            {
            }
        }

        public async Task LoadWeatherData(bool forceApi = false)
        {
            parent.Children.Clear();
            Days.Items.Clear();

            try
            {
                DataResponce weatherData = null;
                bool fromCache = false;

                if (!forceApi)
                {
                    var cachedData = await _cacheService.GetCachedWeatherAsync(currentCity, currentLat, currentLon);
                    if (cachedData != null)
                    {
                        weatherData = cachedData;
                        fromCache = true;
                        responce = weatherData;
                    }
                }

                if (weatherData == null || forceApi)
                {
                    try
                    {
                        weatherData = await GetWeather.Get(currentLat, currentLon, currentCity, forceApi);
                        responce = weatherData;
                        fromCache = false;
                    }
                    catch (Exception apiEx)
                    {
                        if (!forceApi)
                        {
                            weatherData = await _cacheService.GetCachedWeatherAsync(currentCity, currentLat, currentLon);
                            if (weatherData != null)
                            {
                                fromCache = true;
                                responce = weatherData;

                                Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show($"Используются кэшированные данные. API недоступно: {apiEx.Message}",
                                        "Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
                                });
                            }
                            else
                            {
                                throw apiEx;
                            }
                        }
                        else
                        {
                            throw apiEx;
                        }
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    if (responce?.forecasts != null)
                    {
                        foreach (Forecast forecast in responce.forecasts)
                        {
                            Days.Items.Add(forecast.date.ToString("dd.MM.yyyy"));
                        }

                        if (Days.Items.Count > 0)
                        {
                            Days.SelectedIndex = 0;
                        }

                        string sourceInfo = fromCache ? " (из кэша)" : " (актуальные)";
                        LocationText.Text = $"{currentCity}{sourceInfo}";
                    }

                    Create(0);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка загрузки погоды: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public void Create(int idForecast)
        {
            if (responce?.forecasts == null || idForecast < 0 || idForecast >= responce.forecasts.Count)
            {
                return;
            }

            parent.Children.Clear();

            var forecast = responce.forecasts[idForecast];
            if (forecast.hours != null)
            {
                foreach (Hour hour in forecast.hours)
                {
                    parent.Children.Add(new Elements.Item(hour));
                }
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string cityName = CityTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(cityName))
            {
                MessageBox.Show("Введите название города", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var originalContent = SearchButton.Content;
            var originalIsEnabled = SearchButton.IsEnabled;

            try
            {
                SearchButton.IsEnabled = false;
                SearchButton.Content = "Поиск...";

                var coordinates = await Geocoding.GetCoordinates(cityName);
                currentLat = coordinates.lat;
                currentLon = coordinates.lon;
                currentCity = cityName;

                await LoadWeatherData(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска города: {ex.Message}\nПроверьте название и попробуйте снова.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SearchButton.IsEnabled = originalIsEnabled;
                SearchButton.Content = originalContent;
            }
        }

        private void SelectDay(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Days.SelectedIndex >= 0 && responce?.forecasts != null && Days.SelectedIndex < responce.forecasts.Count)
            {
                Create(Days.SelectedIndex);
            }
        }

        private async void UpdateWeather(object sender, RoutedEventArgs e)
        {
            var originalContent = (sender as System.Windows.Controls.Button)?.Content;
            var originalIsEnabled = (sender as System.Windows.Controls.Button)?.IsEnabled ?? true;

            try
            {
                if (sender is System.Windows.Controls.Button button)
                {
                    button.IsEnabled = false;
                    button.Content = "Обновление...";
                }

                await LoadWeatherData(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (sender is System.Windows.Controls.Button button)
                {
                    button.IsEnabled = originalIsEnabled;
                    button.Content = originalContent;
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _cleanupTimer.Stop();
            _cacheService?.Dispose();
            base.OnClosed(e);
        }
    }
}