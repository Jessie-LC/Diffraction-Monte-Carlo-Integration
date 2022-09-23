using System.Runtime.InteropServices;

namespace Diffraction_Monte_Carlo_Integration.UI.Internal;

internal class DMCIWrapper
{
    [DllImport(@"Diffraction Monte-Carlo Integration.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ComputeDiffractionImageExport(int wavelengthCount, bool squareScale, int size, int wavelengthIndex, float quality, float radius, float scale, float dist, float[] Irradiance, float[] Wavelength);
}
