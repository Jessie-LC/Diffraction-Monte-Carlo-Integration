using Diffraction_Monte_Carlo_Integration.UI.Internal;
using Diffraction_Monte_Carlo_Integration.UI.Models;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Diffraction_Monte_Carlo_Integration.UI.ViewModels;

internal class ImageDataEventArgs : EventArgs
{
    public readonly Image<Rgb24> Image;


    public ImageDataEventArgs(Image<Rgb24> image)
    {
        Image = image;
    }
}

internal class MainWindowViewModel : IDisposable
{
    public event EventHandler<ImageDataEventArgs> PreviewImageUpdated;

    private readonly object _imageDataLock;
    private CancellationTokenSource buildTokenSource;
    private Task currentPreviewTask;

    public MainWindowModel Model {get; set;}


    public MainWindowViewModel()
    {
        Model = new MainWindowModel();
        _imageDataLock = new object();
    }

    public void Dispose()
    {
        buildTokenSource?.Dispose();
    }

    public async Task RunAsync(CancellationToken token = default)
    {
        Model.IsRunning = true;
        Model.OutputMessage = null;
        Model.PreviewImage = null;

        buildTokenSource?.Dispose();
        buildTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        var timer = Stopwatch.StartNew();

        var imageData = new SpectralImageData(Model.TextureSize, Model.WavelengthCount);

        try {
            DMCIWrapper.AllocateMemory(Model.MaxThreadCount, Model.TextureSize);

            var wavelengthIndex = -1;
            var taskList = new Task[Model.MaxThreadCount];

            for (var i = 0; i < Model.MaxThreadCount; i++) {
                var taskIndex = i;
                taskList[i] = Task.Run(() => {
                    int w;
                    while ((w = Interlocked.Increment(ref wavelengthIndex)) < Model.WavelengthCount) {
                        buildTokenSource.Token.ThrowIfCancellationRequested();

                        DMCIWrapper.ComputeDiffractionImageExport(taskIndex, Model.WavelengthCount, Model.SquareScale, Model.TextureSize, Model.BladeCount, w, Model.Quality, Model.Radius, Model.Scale, Model.Distance, imageData.Irradiance, imageData.Wavelength);

                        if (currentPreviewTask is { IsCompleted: false }) continue;

                        SpectralImageData snapshot;
                        lock (_imageDataLock) snapshot = imageData.CreateSnapshot();

                        currentPreviewTask = Task.Run(() => {
                            var newImage = ImageBuilder.BuildPreviewImage(snapshot);
                            OnPreviewImageUpdated(newImage);
                        }, buildTokenSource.Token);
                    }
                }, buildTokenSource.Token);
            }

            await Task.WhenAll(taskList);

            if (currentPreviewTask is { IsCompleted: false })
                await currentPreviewTask;

            await Task.Run(() => {
                var newImage = ImageBuilder.BuildPreviewImage(imageData);
                OnPreviewImageUpdated(newImage);
            }, buildTokenSource.Token);

            Model.OutputMessage = $"Duration: {timer.Elapsed:g}";
        }
        catch (OperationCanceledException) {
            Model.OutputMessage = "Cancelled";
            return;
        }
        finally {
            DMCIWrapper.ClearMemory(Model.MaxThreadCount);
            Model.IsRunning = false;
        }

        var saveFileDialog = new SaveFileDialog {
            Filter = "DAT File|*.dat|PNG File|*.png",
            FileName = "diffraction",
        };

        if (saveFileDialog.ShowDialog() != true) return;

        await using var stream = File.Open(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);

        switch (saveFileDialog.FilterIndex) {
            case 2:
                var previewImage = ImageBuilder.BuildPreviewImage(imageData);
                await previewImage.SaveAsPngAsync(stream, token);
                break;
            default:
                await ImageBuilder.BuildRawImageAsync(stream, imageData, token);
                break;
        }
    }

    public void Cancel()
    {
        if (!Model.IsRunning) return;

        Model.OutputMessage = "Cancelling...";
        buildTokenSource?.Cancel();
    }

    protected virtual void OnPreviewImageUpdated(Image<Rgb24> image)
    {
        PreviewImageUpdated?.Invoke(this, new ImageDataEventArgs(image));
    }
}
