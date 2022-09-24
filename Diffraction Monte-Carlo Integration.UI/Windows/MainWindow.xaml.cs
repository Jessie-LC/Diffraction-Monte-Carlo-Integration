using Diffraction_Monte_Carlo_Integration.UI.Internal;
using Diffraction_Monte_Carlo_Integration.UI.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Diffraction_Monte_Carlo_Integration.UI.Windows;

public partial class MainWindow : IDisposable
{
    private readonly object _imageLock;
    private Image<Rgb24> previewImage;


    public MainWindow()
    {
        _imageLock = new object();

        InitializeComponent();
    }

    public void Dispose()
    {
        previewImage?.Dispose();
        ViewModel?.Dispose();
    }

    private async void RunButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunAsync();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.Cancel();
    }

    private void OnBuildProgressChanged(object sender, BuildProgressEventArgs e)
    {
        Dispatcher.Invoke(() => ViewModel.Model.BuildProgress = e.Progress);
    }

    private async void OnPreviewImageUpdated(object sender, ImageDataEventArgs e)
    {
        await Dispatcher.BeginInvoke(() => {
            lock (_imageLock) {
                previewImage?.Dispose();
                previewImage = e.Image;
            }

            var previewImageSource = new ImageSharpSource<Rgb24>(e.Image);
            previewImageSource.Freeze();

            ViewModel.Model.PreviewImage = previewImageSource;
        });
    }
}
