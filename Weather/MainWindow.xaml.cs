using System.Windows;
using Weather.Classes;
using Weather.Models;

namespace Weather
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DataResponce responce;
        public MainWindow()
        {
            InitializeComponent();
            Iint();

        }
        public async void Iint()
        {
            parent.Children.Clear();
            responce = await GetWeather.Get(58.009671f, 56.226184f);
            foreach (Forecast forecast in responce.forecasts)
            {
                Days.Items.Add(forecast.date.ToString("dd.MM.yyyy"));
            }
            Create(0);
        }
        public void Create(int idForecast)
        {
            foreach (Hour hour in responce.forecasts[idForecast].hours)
            {
                parent.Children.Add(new Elements.Item(hour));
            }
        }


        private void SelectDay(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Create(Days.SelectedIndex);
        }

        private void UpdateWeather(object sender, RoutedEventArgs e)
        {
            Iint();
        }
    }
}