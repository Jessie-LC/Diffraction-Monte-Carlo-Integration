using System;
using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Diffraction_Monte_Carlo_Integration.UI.Internal;

internal static class ImageBuilder
{
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
