using Diffraction_Monte_Carlo_Integration.UI.Internal;
using Diffraction_Monte_Carlo_Integration.UI.ViewModels;
using SixLabors.ImageSharp.PixelFormats;
using System.Windows;

namespace Diffraction_Monte_Carlo_Integration.UI.Windows;

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

    private void OnSpectralImageDataUpdated(object sender, SpectralImageDataEventArgs e)
    {
        var previewImage = ImageBuilder.BuildPreviewImage(e.ImageData);

        Dispatcher.BeginInvoke(() => {
            var previewImageSource = new ImageSharpSource<Rgb24>(previewImage);
            previewImageSource.Freeze();
            ViewModel.Model.PreviewImage = previewImageSource;
        });
    }
}
