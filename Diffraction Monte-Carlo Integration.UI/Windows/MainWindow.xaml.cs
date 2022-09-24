using Diffraction_Monte_Carlo_Integration.UI.Internal;
using Diffraction_Monte_Carlo_Integration.UI.ViewModels;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Windows;
using System.Windows.Input;

namespace Diffraction_Monte_Carlo_Integration.UI.Windows;

public partial class MainWindow : IDisposable
{
    private readonly object _imageLock;


    public MainWindow()
    {
        _imageLock = new object();

        InitializeComponent();
    }

    public void Dispose()
    {
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
                ViewModel.Model.PreviewImage?.Dispose();
                ViewModel.Model.PreviewImage = e.Image;
            }

            var previewImageSource = new ImageSharpSource<Rgb24>(e.Image);
            previewImageSource.Freeze();

            ViewModel.Model.PreviewImageSource = previewImageSource;
        });
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var isCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        if (!isCtrl || e.Handled) return;
        e.Handled = true;

        var value = ViewModel.Model.Zoom;
        value += e.Delta * value * 0.001f;
        ViewModel.Model.Zoom = Math.Clamp(value, 0.01f, 100f);
    }
}
