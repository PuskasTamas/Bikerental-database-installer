using System.Windows;
using System.Windows.Media;

namespace Bikerental
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void btnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;
            button.Foreground = new SolidColorBrush(Color.FromRgb(255, 234, 0));
            button.FontWeight = FontWeights.ExtraBold;
        }
        private void btnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;
            button.Foreground = new SolidColorBrush(Color.FromRgb(253, 197, 0));
            button.FontWeight = FontWeights.Bold;
        }
    } 
}
