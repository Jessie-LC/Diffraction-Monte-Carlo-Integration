﻿using System.Runtime.InteropServices;

namespace Diffraction_Monte_Carlo_Integration.UI.Internal;

internal class DMCIWrapper
{
    [DllImport(@"Diffraction Monte-Carlo Integration.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ComputeDiffractionImageExport(int threadIDX, int wavelengthCount, bool squareScale, int size, int bladeCount, int wavelengthIndex, float quality, float radius, float scale, float dist, float[] Irradiance, float[] Wavelength);

    [DllImport(@"Diffraction Monte-Carlo Integration.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void _CreateStreams(int streamCount);

    [DllImport(@"Diffraction Monte-Carlo Integration.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void _DestroyStreams(int streamCount);

    [DllImport(@"Diffraction Monte-Carlo Integration.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AllocateMemory(int threadCount, int size);

    [DllImport(@"Diffraction Monte-Carlo Integration.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ClearMemory(int threadCount);
}
