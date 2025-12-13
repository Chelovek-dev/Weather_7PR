using System.Windows.Controls;
using Weather.Models;

namespace Weather.Elements
{
    /// <summary>
    /// Логика взаимодействия для Item.xaml
    /// </summary>
    public partial class Item : UserControl
    {
        public Item(Hour hour)
        {
            InitializeComponent();

            lHour.Content = hour.hour;
            lCondition.Content = hour.ToCondition();
            lHumidity.Content = hour.humidity + "%";
            lPrecType.Content = hour.ToPrecType();
            lTemp.Content = hour.temp + "°";

        }
    }
}
