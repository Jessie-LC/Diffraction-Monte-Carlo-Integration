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

internal class MainWindowViewModel : IDisposable
{
    public event EventHandler<BuildProgressEventArgs> BuildProgressChanged;
    public event EventHandler PreviewImageUpdated;

    //private readonly object _imageDataLock;
    private CancellationTokenSource buildTokenSource;
    private SpectralImageData currentImageData;
    //private Task currentPreviewTask;

    public MainWindowModel Model {get; set;}


    public MainWindowViewModel()
    {
        Model = new MainWindowModel();
        Model.ExposureChanged += OnModelExposureChanged;

        //_imageDataLock = new object();
    }

    public void Dispose()
    {
        Model.PreviewImage?.Dispose();
        buildTokenSource?.Dispose();
    }

    public async Task RunAsync(CancellationToken token = default)
    {
        Model.IsRunning = true;
        Model.OutputMessage = null;
        Model.PreviewImageSource = null;
        Model.BuildProgress = 0;

        buildTokenSource?.Dispose();
        buildTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        var timer = Stopwatch.StartNew();

        var maxThreadCount = Model.MaxThreadCount ?? MainWindowModel.MaxThreadCountDefault;
        var streamCount = 16;
        var textureSize = Model.TextureSize ?? 256;
        var wavelengthCount = Model.WavelengthCount ?? 30;
        currentImageData = new SpectralImageData(textureSize, wavelengthCount);

        Model.PreviewImage?.Dispose();
        Model.PreviewImage = new Image<Rgb24>(textureSize, textureSize);

        try {
            DMCIWrapper._CreateStreams(streamCount);
            DMCIWrapper.AllocateMemory(maxThreadCount, textureSize);

            var progressIndex = 0;
            var wavelengthIndex = -1;
            var taskList = new Task[maxThreadCount];

            for (var i = 0; i < maxThreadCount; i++) {
                var taskIndex = i;
                taskList[i] = Task.Run(() => {
                    int w;
                    while ((w = Interlocked.Increment(ref wavelengthIndex)) < wavelengthCount) {
                        buildTokenSource.Token.ThrowIfCancellationRequested();

                        var radius = Model.Radius ?? 2f;
                        var scale = Model.Scale ?? 10f;
                        var distance = Model.Distance ?? 10f;
                        var bladeCount = Model.BladeCount ?? 3;

                        DMCIWrapper.ComputeDiffractionImageExport(taskIndex, wavelengthCount, Model.SquareScale, textureSize, bladeCount, w, Model.Quality, radius, scale, distance, currentImageData.Irradiance, currentImageData.Wavelength);

                        currentImageData.AppendFinalColorSlice(w);

                        var progress = Interlocked.Increment(ref progressIndex);
                        OnBuildProgressChanged(progress);

                        //if (currentPreviewTask is { IsCompleted: false }) continue;

                        //currentPreviewTask = Task.Run(() => {
                        //    //SpectralImageData snapshot;
                        //    //lock (_imageDataLock) snapshot = currentImageData.CreateSnapshot();

                        //    var newImage = ImageBuilder.BuildPreviewImage(snapshot, (float?)Model.Exposure ?? 1f);
                        //    OnPreviewImageUpdated(newImage);
                        //}, buildTokenSource.Token);
                        OnPreviewImageUpdated();
                    }
                }, buildTokenSource.Token);
            }

            await Task.WhenAll(taskList);

            //if (currentPreviewTask is { IsCompleted: false })
            //    await currentPreviewTask;

            //await RebuildPreviewImageAsync(currentImageData, buildTokenSource.Token);
            OnPreviewImageUpdated();

            Model.OutputMessage = $"Duration: {timer.Elapsed:g}";
        }
        catch (OperationCanceledException) {
            Model.OutputMessage = "Cancelled";
            return;
        }
        finally {
            DMCIWrapper.ClearMemory(maxThreadCount);
            DMCIWrapper._DestroyStreams(streamCount);
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
                //var previewImage = ImageBuilder.BuildPreviewImage(currentImageData);
                currentImageData.PopulateFinalColorImage(Model.PreviewImage);
                await Model.PreviewImage.SaveAsPngAsync(stream, token);
                break;
            default:
                await ImageBuilder.BuildRawImageAsync(stream, currentImageData, token);
                break;
        }
    }

    public void Cancel()
    {
        if (!Model.IsRunning) return;

        Model.OutputMessage = "Cancelling...";
        buildTokenSource?.Cancel();
    }

    public void UpdatePreviewImage()
    {
        currentImageData.PopulateFinalColorImage(Model.PreviewImage, (float?)Model.Exposure ?? 1f);
    }

    //public Task RebuildPreviewImageAsync(SpectralImageData imageData, CancellationToken token = default)
    //{
    //    return Task.Run(() => {
    //        var newImage = ImageBuilder.BuildPreviewImage(imageData, (float?)Model.Exposure ?? 1f);
    //        OnPreviewImageUpdated(newImage);
    //    }, token);
    //}

    private void OnModelExposureChanged(object sender, EventArgs e)
    {
        if (Model.IsRunning || currentImageData == null) return;

        //await RebuildPreviewImageAsync(currentImageData);
        OnPreviewImageUpdated();
    }

    protected virtual void OnBuildProgressChanged(in int progress)
    {
        BuildProgressChanged?.Invoke(this, new BuildProgressEventArgs(progress));
    }

    protected virtual void OnPreviewImageUpdated()
    {
        PreviewImageUpdated?.Invoke(this, EventArgs.Empty);
    }
}

internal class BuildProgressEventArgs : EventArgs
{
    public readonly int Progress;


    public BuildProgressEventArgs(in int progress)
    {
        Progress = progress;
    }
}

internal class ImageDataEventArgs : EventArgs
{
    public readonly Image<Rgb24> Image;


    public ImageDataEventArgs(Image<Rgb24> image)
    {
        Image = image;
    }
}
