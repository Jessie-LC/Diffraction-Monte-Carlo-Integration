using System.Windows;

namespace Diffraction_Monte_Carlo_Integration.UI.Windows
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_OnClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.RunAsync();
        }
    }
}
