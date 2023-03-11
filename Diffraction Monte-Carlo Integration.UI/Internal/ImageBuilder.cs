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
        Vector3 pixel, pixelD65, d65w, pixelWavelengthValue;
        for (y = 0; y < imageData.Size; y++) {
            for (x = 0; x < imageData.Size; x++) {
                pixel = Vector3.Zero;
                pixelD65 = Vector3.Zero;

                for (w = 0; w < imageData.WavelengthCount; w++) {
                    irradianceIndex = x + y * imageData.Size + w * (imageData.Size * imageData.Size);
                    Spectral.SpectrumToRGB(imageData.Irradiance[irradianceIndex], imageData.Wavelength[w], out pixelWavelengthValue);
                    pixel += pixelWavelengthValue;

                    var d65 = Plancks(6504f, imageData.Wavelength[w]);
                    Spectral.SpectrumToRGB(d65, imageData.Wavelength[w], out d65w);
                    pixelD65 += d65w;
                }

                pixelD65 /= imageData.WavelengthCount;

                pixel /= imageData.WavelengthCount;
                pixel /= pixelD65;

                BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(0, 4), float.IsNaN(pixel.X) ? 0f : pixel.X);
                BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(4, 4), float.IsNaN(pixel.Y) ? 0f : pixel.Y);
                BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(8, 4), float.IsNaN(pixel.Z) ? 0f : pixel.Z);
                await stream.WriteAsync(buffer, 0, 12, token);
            }
        }
    }

    internal static float Plancks(in float t, in float lambda) {
        const float h = 6.62607015e-16f;
        const float c = 2.9e17f;
        const float k = 1.38e-5f;
        //const float c_1L = 2f * h * c * c;
        //const float c_2 = h * c / k;

        var p1 = 2f * h * MathF.Pow(c, 2f) * MathF.Pow(lambda, -5f);
        var p2 = MathF.Exp((h * c) / (lambda * k * t)) - 1f;

        return p1 / p2;
    }
}
