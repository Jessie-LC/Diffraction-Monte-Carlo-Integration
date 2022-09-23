using System;

namespace Diffraction_Monte_Carlo_Integration.UI.Internal;

internal class SpectralImageData
{
    public readonly int Size;
    public readonly int WavelengthCount;
    public readonly float[] Wavelength;
    public readonly float[] Irradiance;


    public SpectralImageData(in int size, in int wavelengthCount)
    {
        if (size < 1) throw new ArgumentOutOfRangeException(nameof(size), "Size value must be greater than zero!");
        if (wavelengthCount < 1) throw new ArgumentOutOfRangeException(nameof(wavelengthCount), "Wavelength-Count value must be greater than zero!");

        Size = size;
        WavelengthCount = wavelengthCount;
        Wavelength = new float[wavelengthCount];
        Irradiance = new float[size * size * wavelengthCount];
    }

    private SpectralImageData(in SpectralImageData imageData)
    {
        Size = imageData.Size;
        WavelengthCount = imageData.WavelengthCount;
        Wavelength = (float[])imageData.Wavelength.Clone();
        Irradiance = imageData.Irradiance;
    }

    /// <summary>
    /// Creates a copy of the current SpectralImageData using a copy of the wavelength buffer.
    /// This allows the same irradiance buffer to continue being updated for additional wavelengths,
    /// while providing a thread-safe way to read only the existing wavelength values.
    /// </summary>
    public SpectralImageData CreateSnapshot()
    {
        return new SpectralImageData(this);
    }
}
