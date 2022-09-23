using Diffraction_Monte_Carlo_Integration.UI.Internal;
using Diffraction_Monte_Carlo_Integration.UI.Models;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    //private SpectralImageData imageData;

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

        var options = new ParallelOptions {
            MaxDegreeOfParallelism = Model.MaxThreadCount,
            CancellationToken = tokenSource.Token,
        };

        var imageData = new SpectralImageData(Model.TextureSize, Model.WavelengthCount);

        try {
            await Parallel.ForEachAsync(Enumerable.Range(0, Model.WavelengthCount), options, async (w, t) => {
                await Task.Run(() => {
                    DMCIWrapper.ComputeDiffractionImageExport(Model.WavelengthCount, Model.SquareScale, Model.TextureSize, w, Model.Quality, Model.Radius, Model.Scale, Model.Distance, imageData.Irradiance, imageData.Wavelength);
                }, t);

                //var previewImage = BuildPreviewImage(wavelengthBuffer, irradianceBuffer);
                OnSpectralImageDataUpdated(imageData);
            });

            Model.OutputMessage = $"Duration: {timer.Elapsed:g}";
        }
        catch (OperationCanceledException) {
            Model.OutputMessage = "Cancelled";
            return;
        }
        finally {
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
