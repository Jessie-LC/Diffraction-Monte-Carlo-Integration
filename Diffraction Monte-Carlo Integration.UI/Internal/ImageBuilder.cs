using System;
using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Diffraction_Monte_Carlo_Integration.UI.Internal;

internal static class ImageBuilder
{
    //public static Image<Rgb24> BuildPreviewImage(SpectralImageData imageData, float exposure = 1f)
    //{
    //    var image = new Image<Rgb24>(Configuration.Default, imageData.Size, imageData.Size);

    //    var wavelengthList = imageData.Wavelength.Where(w => w > float.Epsilon).ToArray();

    //    try {
    //        image.Mutate(context => {
    //            context.ProcessPixelRowsAsVector4((row, pos) => {
    //                Vector3 pixel, pixelWavelengthResult;
    //                int x, w, irradianceIndex;
    //                for (x = 0; x < imageData.Size; x++) {
    //                    pixel = Vector3.Zero;

    //                    for (w = 0; w < wavelengthList.Length; w++) {
    //                        irradianceIndex = x + pos.Y * imageData.Size + w * (imageData.Size * imageData.Size);
    //                        Spectral.SpectrumToRGB(in imageData.Irradiance[irradianceIndex], in wavelengthList[w], out pixelWavelengthResult);
    //                        pixel += pixelWavelengthResult;
    //                    }

    //                    pixel /= wavelengthList.Length;
    //                    pixel *= exposure;

    //                    row[x].X = pixel.X;
    //                    row[x].Y = pixel.Y;
    //                    row[x].Z = pixel.Z;
    //                }
    //            });
    //        });

    //        return image;
    //    }
    //    catch {
    //        image.Dispose();
    //        throw;
    //    }
    //}

    public static async Task BuildRawImageAsync(Stream stream, SpectralImageData imageData, CancellationToken token)
    {
        var buffer = new byte[12];

        int x, y, w, irradianceIndex;
        Vector3 pixel, pixelWavelengthValue;
        for (y = 0; y < imageData.Size; y++) {
            for (x = 0; x < imageData.Size; x++) {
                pixel = Vector3.Zero;

                for (w = 0; w < imageData.WavelengthCount; w++) {
                    irradianceIndex = x + y * imageData.Size + w * (imageData.Size * imageData.Size);
                    Spectral.SpectrumToRGB(imageData.Irradiance[irradianceIndex], imageData.Wavelength[w], out pixelWavelengthValue);
                    pixel += pixelWavelengthValue;
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
