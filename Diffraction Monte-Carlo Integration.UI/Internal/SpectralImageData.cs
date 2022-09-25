using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Numerics;

namespace Diffraction_Monte_Carlo_Integration.UI.Internal;

public class SpectralImageData
{
    private readonly object _finalColorLock;

    public readonly int Size;
    public readonly int WavelengthCount;
    public readonly float[] Wavelength;
    public readonly float[] Irradiance;
    public readonly Vector3[,] FinalColor;


    public SpectralImageData(in int size, in int wavelengthCount)
    {
        if (size < 1) throw new ArgumentOutOfRangeException(nameof(size), "Size value must be greater than zero!");
        if (wavelengthCount < 1) throw new ArgumentOutOfRangeException(nameof(wavelengthCount), "Wavelength-Count value must be greater than zero!");

        Size = size;
        WavelengthCount = wavelengthCount;
        Wavelength = new float[wavelengthCount];
        Irradiance = new float[size * size * wavelengthCount];
        FinalColor = new Vector3[size, size];

        _finalColorLock = new object();
    }

    public void AppendFinalColorSlice(int wavelengthIndex)
    {
        var wavelengthOffset = wavelengthIndex * (Size * Size);

        lock (_finalColorLock) {
            for (var y = 0; y < Size; y++) {
                for (var x = 0; x < Size; x++) {
                    var irradianceIndex = x + y * Size + wavelengthOffset;
                    Spectral.SpectrumToRGB(in Irradiance[irradianceIndex], in Wavelength[wavelengthIndex], out var pixelRGB);
                    FinalColor[x, y] += pixelRGB / WavelengthCount;
                }
            }
        }
    }

    public void PopulateFinalColorImage<TPixel>(Image<TPixel> image, float exposure = 1f)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        image.Mutate(context => {
            context.ProcessPixelRowsAsVector4((row, region) => {
                for (var x = 0; x < Size; x++) {
                    row[x].X = FinalColor[x, region.Y].X * exposure;
                    row[x].Y = FinalColor[x, region.Y].Y * exposure;
                    row[x].Z = FinalColor[x, region.Y].Z * exposure;
                }
            });
        });
    }
}
