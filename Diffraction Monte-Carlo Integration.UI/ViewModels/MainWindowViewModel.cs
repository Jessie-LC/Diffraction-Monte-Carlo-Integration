using Diffraction_Monte_Carlo_Integration.UI.Internal;
using Diffraction_Monte_Carlo_Integration.UI.Models;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Diffraction_Monte_Carlo_Integration.UI.ViewModels;

internal class SpectralImageDataEventArgs : EventArgs
{
    public SpectralImageData ImageData;
}

internal class MainWindowViewModel : IDisposable
{
    public event EventHandler<SpectralImageDataEventArgs> SpectralImageDataUpdated;

    private CancellationTokenSource tokenSource;

    public MainWindowModel Model {get; set;}


    public MainWindowViewModel()
    {
        Model = new MainWindowModel();
    }

    public void Dispose()
    {
        tokenSource?.Dispose();
    }

    public async Task RunAsync(CancellationToken token = default)
    {
        Model.IsRunning = true;
        Model.OutputMessage = null;
        Model.PreviewImage = null;

        tokenSource?.Dispose();
        tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        var timer = Stopwatch.StartNew();

        var imageData = new SpectralImageData(Model.TextureSize, Model.WavelengthCount);

        try {
            DMCIWrapper.AllocateMemory(Model.MaxThreadCount, Model.TextureSize);

            var wavelengthIndex = -1;
            var taskList = new Task[Model.MaxThreadCount];

            for (var i = 0; i < Model.MaxThreadCount; i++) {
                var ii = i;
                taskList[i] = Task.Run(() => {
                    int w;
                    while ((w = Interlocked.Increment(ref wavelengthIndex)) < Model.WavelengthCount) {
                        tokenSource.Token.ThrowIfCancellationRequested();

                        DMCIWrapper.ComputeDiffractionImageExport(ii, Model.WavelengthCount, Model.SquareScale, Model.TextureSize, w, Model.Quality, Model.Radius, Model.Scale, Model.Distance, imageData.Irradiance, imageData.Wavelength);

                        OnSpectralImageDataUpdated(imageData);
                    }
                }, tokenSource.Token);
            }

            await Task.WhenAll(taskList);

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
        tokenSource?.Cancel();
    }

    protected virtual void OnSpectralImageDataUpdated(SpectralImageData imageData)
    {
        SpectralImageDataUpdated?.Invoke(this, new SpectralImageDataEventArgs {
            ImageData = imageData.CreateSnapshot(),
        });
    }
}
