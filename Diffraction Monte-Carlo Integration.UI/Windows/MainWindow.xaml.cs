using Diffraction_Monte_Carlo_Integration.UI.Internal;
using Diffraction_Monte_Carlo_Integration.UI.ViewData;
using Diffraction_Monte_Carlo_Integration.UI.ViewModels;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Diffraction_Monte_Carlo_Integration.UI.Windows;

public partial class MainWindow : IDisposable
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void Dispose()
    {
        ViewModel?.Dispose();
    }

    private async Task UpdatePreviewImageAsync()
    {
        await Dispatcher.BeginInvoke(() => {
            ViewModel.UpdatePreviewImage();

            var previewImageSource = new ImageSharpSource<Rgb24>(ViewModel.Model.PreviewImage);
            previewImageSource.Freeze();

            ViewModel.Model.PreviewImageSource = previewImageSource;
        });
    }

    private async void RunButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunAsync();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.Cancel();
    }

    private async void OnBuildProgressChanged(object sender, BuildProgressEventArgs e)
    {
        await Dispatcher.BeginInvoke(() => ViewModel.Model.BuildProgress = e.Progress);
    }

    private async void OnPreviewImageUpdated(object sender, EventArgs e)
    {
        await UpdatePreviewImageAsync();
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

    private void OnPresetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var preset = (PresetValues.Item)presetDropDown.SelectedItem;
        if (preset == null) return;

        ViewModel.Model.WavelengthCount = preset.WavelengthCount;
        ViewModel.Model.Quality = preset.Quality;
        ViewModel.Model.Radius = preset.Radius;
        ViewModel.Model.Scale = preset.Scale;
        ViewModel.Model.Distance = preset.Distance;
        ViewModel.Model.BladeCount = preset.BladeCount;
    }
}
