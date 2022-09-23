using Diffraction_Monte_Carlo_Integration.UI.Internal;
using Diffraction_Monte_Carlo_Integration.UI.Models;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Diffraction_Monte_Carlo_Integration.UI.ViewModels
{
    internal class MainWindowViewModel : IDisposable
    {
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
            var timer = Stopwatch.StartNew();

            tokenSource?.Dispose();
            tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            var options = new ParallelOptions {
                MaxDegreeOfParallelism = Model.MaxThreadCount,
                CancellationToken = tokenSource.Token,
            };

            var wavelengthBuffer = new float[Model.WavelengthCount];
            var irradianceBuffer = new float[Model.TextureSize * Model.TextureSize * Model.WavelengthCount];

            try {
                await Parallel.ForEachAsync(Enumerable.Range(0, Model.WavelengthCount), options, async (w, t) => {
                    await Task.Run(() => {
                        DMCIWrapper.ComputeDiffractionImageExport(Model.WavelengthCount, Model.SquareScale, Model.TextureSize, w, Model.Quality, Model.Radius, Model.Scale, Model.Distance, irradianceBuffer, wavelengthBuffer);
                    }, t);

                    var previewImage = BuildPreviewImage(wavelengthBuffer, irradianceBuffer);
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
                    var previewImage = BuildPreviewImage(wavelengthBuffer, irradianceBuffer);
                    await previewImage.SaveAsPngAsync(stream, token);
                    break;
                default:
                    await BuildFinalImageAsync(stream, wavelengthBuffer, irradianceBuffer, token);
                    break;
            }
        }

        public void Cancel()
        {
            if (!Model.IsRunning) return;

            Model.OutputMessage = "Cancelling...";
            tokenSource?.Cancel();
        }

        private Image BuildPreviewImage(IReadOnlyList<float> wavelengthBuffer, IReadOnlyList<float> irradianceBuffer)
        {
            var image = new Image<Rgb48>(Configuration.Default, Model.TextureSize, Model.TextureSize);

            try {
                image.Mutate(context => {
                    context.ProcessPixelRowsAsVector4((row, pos) => {
                        for (var x = 0; x < Model.TextureSize; x++) {
                            var pixel = Vector3.Zero;

                            for (var w = 0; w < Model.WavelengthCount; w++) {
                                var irradianceIndex = x + pos.Y * Model.TextureSize + w * (Model.TextureSize * Model.TextureSize);
                                pixel += Spectral.SpectrumToRGB(irradianceBuffer[irradianceIndex] * 1e8f, wavelengthBuffer[w]);
                            }

                            row[x].X = pixel.X / Model.WavelengthCount;
                            row[x].Y = pixel.Y / Model.WavelengthCount;
                            row[x].Z = pixel.Z / Model.WavelengthCount;
                        }
                    });
                });

                return image;
            }
            catch {
                image.Dispose();
                throw;
            }
        }

        private async Task BuildFinalImageAsync(Stream stream, IReadOnlyList<float> wavelengthBuffer, IReadOnlyList<float> irradianceBuffer, CancellationToken token)
        {
            var buffer = new byte[12];

            for (var y = 0; y < Model.TextureSize; y++) {
                for (var x = 0; x < Model.TextureSize; x++) {
                    var pixel = Vector3.Zero;

                    for (var w = 0; w < Model.WavelengthCount; ++w) {
                        var index = x + y * Model.TextureSize + w * (Model.TextureSize * Model.TextureSize);
                        pixel += Spectral.SpectrumToRGB(irradianceBuffer[index] * 1e8f, wavelengthBuffer[w]);
                    }

                    pixel /= Model.WavelengthCount;

                    BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(0, 4), float.IsNaN(pixel.X) ? 0f : pixel.X);
                    BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(4, 4), float.IsNaN(pixel.Y) ? 0f : pixel.Y);
                    BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(8, 4), float.IsNaN(pixel.Z) ? 0f : pixel.Z);
                    await stream.WriteAsync(buffer, 0, 12, token);
                }
            }
        }
    }
}
