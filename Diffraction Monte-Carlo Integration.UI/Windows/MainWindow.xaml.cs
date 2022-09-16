using Diffraction_Monte_Carlo_Integration.UI.Internal;
using System.Windows;

namespace Diffraction_Monte_Carlo_Integration.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var result = DMCIWrapper.ComputeDiffractionImageExport(256, 1.0f, 2.0f, 10.0f, 10.0f);
            MessageBox.Show($"RESULT: {result}");
        }
    }
}
