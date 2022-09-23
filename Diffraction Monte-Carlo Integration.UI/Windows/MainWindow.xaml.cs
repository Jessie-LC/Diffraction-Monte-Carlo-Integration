using System.Windows;

namespace Diffraction_Monte_Carlo_Integration.UI.Windows
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void RunButton_OnClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.RunAsync();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Cancel();
        }
    }
}
