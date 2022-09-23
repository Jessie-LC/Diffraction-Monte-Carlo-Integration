using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Diffraction_Monte_Carlo_Integration.UI.Internal;

internal static class ImageBuilder
{
    public static Image<Rgb24> BuildPreviewImage(SpectralImageData imageData)
    {
        var image = new Image<Rgb24>(Configuration.Default, imageData.Size, imageData.Size);

        try {
            image.Mutate(context => {
                context.ProcessPixelRowsAsVector4((row, pos) => {
                    for (var x = 0; x < imageData.Size; x++) {
                        var pixel = Vector3.Zero;

                        var finalWavelengthCount = 0;
                        for (var w = 0; w < imageData.WavelengthCount; w++) {
                            if (imageData.Wavelength[w] <= float.Epsilon) continue;

                            var irradianceIndex = x + pos.Y * imageData.Size + w * (imageData.Size * imageData.Size);
                            pixel += Spectral.SpectrumToRGB(imageData.Irradiance[irradianceIndex], imageData.Wavelength[w]);
                            finalWavelengthCount++;
                        }

                        pixel /= finalWavelengthCount;

                        row[x].X = pixel.X;
                        row[x].Y = pixel.Y;
                        row[x].Z = pixel.Z;
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

    public static async Task BuildRawImageAsync(Stream stream, SpectralImageData imageData, CancellationToken token)
    {
        var buffer = new byte[12];

        for (var y = 0; y < imageData.Size; y++) {
            for (var x = 0; x < imageData.Size; x++) {
                var pixel = Vector3.Zero;

                for (var w = 0; w < imageData.WavelengthCount; w++) {
                    var index = x + y * imageData.Size + w * (imageData.Size * imageData.Size);
                    pixel += Spectral.SpectrumToRGB(imageData.Irradiance[index], imageData.Wavelength[w]);
                }

                pixel /= imageData.WavelengthCount;

                BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(0, 4), float.IsNaN(pixel.X) ? 0f : pixel.X);
                BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(4, 4), float.IsNaN(pixel.Y) ? 0f : pixel.Y);
                BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(8, 4), float.IsNaN(pixel.Z) ? 0f : pixel.Z);
                await stream.WriteAsync(buffer, 0, 12, token);
            }
        }
    }
}
