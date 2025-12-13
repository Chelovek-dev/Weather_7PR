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

        }
        public async void Iint()
        {
            responce = await GetWeather.Get(58.009671f, 56.226184f);
            Create(0);
        }
        public void Create(int idForecast)
        {
            foreach (Hour hour in responce.forecasts[idForecast].hours)
            {
                parent.Children.Add(new Elements.Item(hour));
            }
        }
    }
}